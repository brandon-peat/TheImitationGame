using Moq;
using Microsoft.AspNetCore.SignalR;
using TheImitationGame.Api.Hubs;
using TheImitationGame.Api.Models;
using TheImitationGame.Api.Interfaces;

namespace TheImitationGame.Tests
{
    public class GameHubTests
    {
        private readonly GameHub hub;
        private readonly Mock<IHubCallerClients> mockClients = new();
        private readonly Mock<IGroupManager> mockGroups = new();
        private readonly Mock<IGamesStore> mockGamesStore = new();
        private readonly Mock<HubCallerContext> mockContext = new();

        private readonly string connectionId = "test-connection-id";
        private readonly string hostConnectionId = "host-connection-id";
        private readonly string joinerConnectionId = "joiner-connection-id";

        private readonly string prompt = "A cat not exploding";

        public GameHubTests()
        {
            mockContext.Setup(context => context.ConnectionId).Returns(connectionId);

            hub = new GameHub(mockGamesStore.Object)
            {
                Clients = mockClients.Object,
                Groups = mockGroups.Object,
                Context = mockContext.Object
            };
        }

        [Fact]
        public async Task CreateGame_WhenCalled_AddsGameAndAddsCallerToGroupWithCorrectId()
        {
            // Arrange
            mockGamesStore.Setup(games => games.TryAdd(connectionId, It.IsAny<Game>())).Returns(true);

            // Act
            var result = await hub.CreateGame();

            // Assert
            Assert.Equal(connectionId, result);
            mockGroups.Verify(
                g => g.AddToGroupAsync(connectionId, connectionId, default),
                Times.Once
            );
        }

        [Fact]
        public async Task CreateGame_WhenGameAlreadyExists_ThrowsWithAlreadyCreatedGameError()
        {
            // Arrange
            mockGamesStore.Setup(games => games.TryAdd(connectionId, It.IsAny<Game>())).Returns(false);

            // Act
            async Task<string> act() => await hub.CreateGame();

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.CreateGame_AlreadyCreatedGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task CreateGame_HavingJoinedOtherGame_ThrowsWithAlreadyJoinedGameError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                .Returns(new KeyValuePair<string, Game>(
                    hostConnectionId,
                    new Game(hostConnectionId, connectionId)
                ));

            // Act
            async Task<string> act() => await hub.CreateGame();

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.CreateGame_AlreadyJoinedGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task LeaveGame_WhenHostForEmptyGame_RemovesGameAndRemovesHostFromGroup()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryRemove(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out string? value) => {
                    value = null;
                    return true;
                });

            // Act
            await hub.LeaveGame();

            // Assert
            mockGamesStore.Verify(
                store => store.TryRemove(connectionId, out It.Ref<Game?>.IsAny),
                Times.Once
            );
            mockGroups.Verify(
                groups => groups.RemoveFromGroupAsync(connectionId, connectionId, default),
                Times.Once
            );
        }

        [Fact]
        public async Task LeaveGame_WhenHostForFilledGame_RemovesJoinerFromGroupAndNotifiesJoiner()
        {
            // Arrange
            var mockJoinerClient = new Mock<ISingleClientProxy>();

            mockGamesStore
                .Setup(games => games.TryRemove(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? game) =>
                {
                    game = new Game(hostConnectionId, joinerConnectionId);
                    return true;
                });
            mockClients
                .Setup(clients => clients.Client(joinerConnectionId))
                .Returns(mockJoinerClient.Object);

            // Act
            await hub.LeaveGame();

            // Assert
            mockGroups.Verify(
                groups => groups.RemoveFromGroupAsync(joinerConnectionId, connectionId, default),
                Times.Once
            );
            mockJoinerClient.Verify(
                client => client.SendCoreAsync(
                    "HostLeft",
                    It.Is<object?[]>(args => args.Length == 0),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task LeaveGame_WhenJoinerForGame_RemovesGameAndRemovesBothMembersFromGroupAndNotifiesHost()
        {
            // Arrange
            var mockHostClient = new Mock<ISingleClientProxy>();

            mockGamesStore
                .Setup(games => games.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                .Returns(new KeyValuePair<string, Game>(
                    hostConnectionId,
                    new Game(hostConnectionId, connectionId)
                ));
            mockClients
                .Setup(clients => clients.Client(hostConnectionId))
                .Returns(mockHostClient.Object);

            // Act
            await hub.LeaveGame();

            //Assert
            mockGamesStore.Verify(
                store => store.TryRemove(hostConnectionId, out It.Ref<Game?>.IsAny),
                Times.Once
            );
            mockGroups.Verify(
                groups => groups.RemoveFromGroupAsync(hostConnectionId, hostConnectionId, default),
                Times.Once
            );
            mockGroups.Verify(
                groups => groups.RemoveFromGroupAsync(connectionId, hostConnectionId, default),
                Times.Once
            );
            mockHostClient.Verify(
                client => client.SendCoreAsync(
                    "JoinerLeft",
                    It.Is<object?[]>(args => args.Length == 0),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task LeaveGame_WhenNotInAnyGame_DoesNothing()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryRemove(connectionId, out It.Ref<Game?>.IsAny))
                .Returns(false);
            mockGamesStore
                .Setup(games => games.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                .Returns(default(KeyValuePair<string, Game>));

            // Act
            var exception = await Record.ExceptionAsync(() => hub.LeaveGame());

            // Assert
            Assert.Null(exception);
            mockGamesStore.Verify(
                store => store.TryRemove(connectionId, out It.Ref<Game?>.IsAny),
                Times.Once
            );
            mockGroups.VerifyNoOtherCalls();
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task JoinGame_WithValidGameId_AddsJoinerToGameAndGroupAndNotifiesHost()
        {
            // Arrange
            var mockHostClient = new Mock<ISingleClientProxy>();

            mockGamesStore
                .Setup(games => games.TryGetValue(hostConnectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? game) =>
                {
                    game = new Game(hostConnectionId);
                    return true;
                });
            mockGamesStore
                .Setup(games => games.TryUpdate(hostConnectionId, It.IsAny<Game>(), It.IsAny<Game>()))
                .Returns(true);
            mockClients
                .Setup(clients => clients.Client(hostConnectionId))
                .Returns(mockHostClient.Object);

            // Act
            await hub.JoinGame(hostConnectionId);

            // Assert
            mockGamesStore.Verify(
                store => store.TryUpdate(hostConnectionId, It.IsAny<Game>(), It.IsAny<Game>()),
                Times.Once
            );
            mockGroups.Verify(
                groups => groups.AddToGroupAsync(connectionId, hostConnectionId, default),
                Times.Once
            );
            mockHostClient.Verify(
                client => client.SendCoreAsync(
                    "GameJoined",
                    It.Is<object?[]>(args => args.Length == 0),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task JoinGame_WhenAlreadyInGame_ThrowsWithAlreadyJoinedGameError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.Any(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                .Returns(true);

            // Act
            async Task act() => await hub.JoinGame(hostConnectionId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_AlreadyJoinedGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WithInvalidGameCode_ThrowsWithGameNotFoundError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryGetValue(hostConnectionId, out It.Ref<Game?>.IsAny))
                .Returns(false);

            // Act
            async Task act() => await hub.JoinGame(hostConnectionId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_GameNotFound.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WithOwnGameCode_ThrowsWithCannotJoinOwnGameError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out string? joiner) =>
                {
                    joiner = null;
                    return true;
                });

            // Act
            async Task act() => await hub.JoinGame(connectionId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_CannotJoinOwnGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WhenGameIsFull_ThrowsWithGameFullError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryGetValue(hostConnectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? game) =>
                {
                    game = new Game(hostConnectionId, joinerConnectionId);
                    return true;
                });

            // Act
            async Task act() => await hub.JoinGame(hostConnectionId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_GameFull.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WithNullGameId_ThrowsWithGameNotFoundError()
        {
            // Arrange
            string? gameId = null;

            // Act
            async Task act() => await hub.JoinGame(gameId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_GameNotFound.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WithEmptyGameId_ThrowsWithGameNotFoundError()
        {
            // Arrange
            string? gameId = "";

            // Act
            async Task act() => await hub.JoinGame(gameId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_GameNotFound.ToString(), ex.Message);
        }

        [Fact]
        public async Task StartGame_WithValidJoinedGameAndHostFirst_SetsGameStateToPromptingAndNotifiesPlayers()
        {
            // Arrange
            var game = new Game(connectionId, joinerConnectionId);
            Game? updatedGame = null;

            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = game;
                    return true;
                });
            mockGamesStore
                .Setup(games => games.TryUpdate(connectionId, It.IsAny<Game>(), It.IsAny<Game>()))
                .Callback((string key, Game newValue, Game _) => updatedGame = newValue)
                .Returns(true);

            var mockHostClient = new Mock<ISingleClientProxy>();
            var mockJoinerClient = new Mock<ISingleClientProxy>();
            mockClients
                .Setup(clients => clients.Client(connectionId))
                .Returns(mockHostClient.Object);
            mockClients
                .Setup(clients => clients.Client(joinerConnectionId))
                .Returns(mockJoinerClient.Object);

            // Act
            await hub.StartGame(true);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Prompting, updatedGame.State);
            mockClients.Verify(
                clients => clients.Client(connectionId).SendCoreAsync(
                    "PromptTimerStarted",
                    It.Is<object?[]>(args => args.Length == 1),
                    default
                ),
                Times.Once
            );
            mockClients.Verify(
                clients => clients.Client(joinerConnectionId).SendCoreAsync(
                    "AwaitPrompt",
                    It.Is<object?[]>(args => args.Length == 0),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task StartGame_WithValidJoinedGameAndJoinerFirst_SetsGameStateToPromptingAndNotifiesPlayers()
        {
            // Arrange
            var game = new Game(connectionId, joinerConnectionId);
            Game? updatedGame = null;

            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = game;
                    return true;
                });
            mockGamesStore
                .Setup(games => games.TryUpdate(connectionId, It.IsAny<Game>(), It.IsAny<Game>()))
                .Callback((string key, Game newValue, Game _) => updatedGame = newValue)
                .Returns(true);

            var mockClient = new Mock<ISingleClientProxy>();
            var mockJoinerClient = new Mock<ISingleClientProxy>();
            mockClients
                .Setup(clients => clients.Client(connectionId))
                .Returns(mockClient.Object);
            mockClients
                .Setup(clients => clients.Client(joinerConnectionId))
                .Returns(mockJoinerClient.Object);

            // Act
            await hub.StartGame(false);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Prompting, updatedGame.State);
            mockClients.Verify(
                clients => clients.Client(connectionId).SendCoreAsync(
                    "AwaitPrompt",
                    It.Is<object?[]>(args => args.Length == 0),
                    default
                ),
                Times.Once
            );
            mockClients.Verify(
                clients => clients.Client(joinerConnectionId).SendCoreAsync(
                    "PromptTimerStarted",
                    It.Is<object?[]>(args => args.Length == 1),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task StartGame_WithNoHostedGame_ThrowsWithNoGameToStartError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = null;
                    return false;
                });

            // Act
            async Task act() => await hub.StartGame(false);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.StartGame_NoGameToStart.ToString(), ex.Message);
        }

        [Fact]
        public async Task StartGame_WithNoJoiner_ThrowsWithNoJoinerInGameError()
        {
            // Arrange
            var game = new Game(connectionId);

            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = game;
                    return true;
                });

            // Act
            async Task act() => await hub.StartGame(false);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.StartGame_NoJoinerInGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task StartGame_WithAlreadyStartedGame_ThrowsWithAlreadyStartedGameError()
        {
            // Arrange
            var game = new Game(connectionId, joinerConnectionId, GameState.Prompting);

            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = game;
                    return true;
                });

            // Act
            async Task act() => await hub.StartGame(false);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.StartGame_AlreadyStartedGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task SubmitPrompt_WithHostAsPrompter_SetsGameStateToDrawingAndSetsPromptAndNotifiesPlayers()
        {
            // Arrange
            var game = new Game(
                hostConnectionId: connectionId,
                joinerConnectionId: joinerConnectionId,
                state: GameState.Prompting,
                prompter: Role.Host);
            Game? updatedGame = null;

            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = game;
                    return true;
                });

            mockGamesStore
                .Setup(games => games.TryUpdate(connectionId, It.IsAny<Game>(), It.IsAny<Game>()))
                .Callback((string key, Game newValue, Game _) => updatedGame = newValue)
                .Returns(true);

            var mockClient = new Mock<ISingleClientProxy>();
            var mockJoinerClient = new Mock<ISingleClientProxy>();
            mockClients
                .Setup(clients => clients.Client(connectionId))
                .Returns(mockClient.Object);
            mockClients
                .Setup(clients => clients.Client(joinerConnectionId))
                .Returns(mockJoinerClient.Object);

            // Act
            await hub.SubmitPrompt(prompt);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Drawing, updatedGame.State);
            mockClients.Verify(
                clients => clients.Client(connectionId).SendCoreAsync(
                    "AwaitDrawings",
                    It.Is<object?[]>(args => args.Length == 0),
                    default
                ),
                Times.Once
            );
            mockClients.Verify(
                clients => clients.Client(joinerConnectionId).SendCoreAsync(
                    "DrawTimerStarted",
                    It.Is<object?[]>(args => args.Length == 1),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SubmitPrompt_WithJoinerAsPrompter_SetsGameStateToDrawingAndSetsPromptAndNotifiesPlayers()
        {
            // Arrange
            var game = new Game(
                hostConnectionId: hostConnectionId,
                joinerConnectionId: connectionId,
                state: GameState.Prompting,
                prompter: Role.Joiner);
            Game? updatedGame = null;

            mockGamesStore
                .Setup(games => games.TryGetValue(hostConnectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = null;
                    return false;
                });
            mockGamesStore
                .Setup(games => games.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                .Returns(new KeyValuePair<string, Game>(hostConnectionId, game));

            mockGamesStore
                .Setup(games => games.TryUpdate(hostConnectionId, It.IsAny<Game>(), It.IsAny<Game>()))
                .Callback((string key, Game newValue, Game _) => updatedGame = newValue)
                .Returns(true);

            var mockHostClient = new Mock<ISingleClientProxy>();
            var mockClient = new Mock<ISingleClientProxy>();
            mockClients
                .Setup(clients => clients.Client(hostConnectionId))
                .Returns(mockHostClient.Object);
            mockClients
                .Setup(clients => clients.Client(connectionId))
                .Returns(mockClient.Object);

            // Act
            await hub.SubmitPrompt(prompt);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Drawing, updatedGame.State);
            mockClients.Verify(
                clients => clients.Client(hostConnectionId).SendCoreAsync(
                    "DrawTimerStarted",
                    It.Is<object?[]>(args => args.Length == 1),
                    default
                ),
                Times.Once
            );
            mockClients.Verify(
                clients => clients.Client(connectionId).SendCoreAsync(
                    "AwaitDrawings",
                    It.Is<object?[]>(args => args.Length == 0),
                    default
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task SubmitPrompt_WhenNotInGame_ThrowsWithNotInAGameError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = null;
                    return false;
                });
            mockGamesStore
                .Setup(games => games.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                .Returns(default(KeyValuePair<string, Game>));

            // Act
            async Task act() => await hub.SubmitPrompt(prompt);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.SubmitPrompt_NotInAGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task SubmitPrompt_WhenNotInPromptingState_ThrowsWithNotInPromptingPhaseError()
        {
            // Arrange
            var game = new Game(
                hostConnectionId: connectionId,
                joinerConnectionId: joinerConnectionId,
                state: GameState.NotStarted,
                prompter: Role.Host);

            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = game;
                    return true;
                });

            // Act
            async Task act() => await hub.SubmitPrompt(prompt);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.SubmitPrompt_NotInPromptingPhase.ToString(), ex.Message);
        }

        [Fact]
        public async Task SubmitPrompt_WhenNotPrompter_ThrowsWithNotPrompterError()
        {
            // Arrange
            var game = new Game(
                hostConnectionId: connectionId,
                joinerConnectionId: joinerConnectionId,
                state: GameState.Prompting,
                prompter: Role.Joiner);

            mockGamesStore
                .Setup(games => games.TryGetValue(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = game;
                    return true;
                });

            // Act
            async Task act() => await hub.SubmitPrompt(prompt);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.SubmitPrompt_NotPrompter.ToString(), ex.Message);
        }
    }
}