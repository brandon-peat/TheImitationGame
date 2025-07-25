using Moq;
using Microsoft.AspNetCore.SignalR;
using TheImitationGame.Api.Hubs;
using TheImitationGame.Api.Models;

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
        public async Task CreateGame_WhenGameAlreadyExists_ThrowsGameHubExceptionWithCorrectErrorCode()
        {
            // Arrange
            mockGamesStore.Setup(games => games.TryAdd(connectionId, It.IsAny<Game>())).Returns(false);

            // Act
            async Task<string> act() => await hub.CreateGame();

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.AlreadyCreatedGame.ToString(), ex.Message);
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
        public async Task JoinGame_WhenAlreadyInGame_ThrowsGameHubExceptionWithAlreadyJoinedGameError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.Any(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                .Returns(true);

            // Act
            async Task act() => await hub.JoinGame(hostConnectionId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.AlreadyJoinedGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WithInvalidGameCode_ThrowsGameHubExceptionWithGameNotFoundError()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryGetValue(hostConnectionId, out It.Ref<Game?>.IsAny))
                .Returns(false);

            // Act
            async Task act() => await hub.JoinGame(hostConnectionId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.GameNotFound.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WithOwnGameCode_ThrowsGameHubExceptionWithCannotJoinOwnGameError()
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
            Assert.Contains(GameHubErrorCode.CannotJoinOwnGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WhenGameIsFull_ThrowsGameHubExceptionWithGameFullError()
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
            Assert.Contains(GameHubErrorCode.GameFull.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WithNullGameId_ThrowsGameNotFoundException()
        {
            // Arrange
            string? gameId = null;

            // Act
            async Task act() => await hub.JoinGame(gameId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.GameNotFound.ToString(), ex.Message);
        }

        [Fact]
        public async Task JoinGame_WithEmptyGameId_ThrowsGameNotFoundException()
        {
            // Arrange
            string? gameId = "";

            // Act
            async Task act() => await hub.JoinGame(gameId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.GameNotFound.ToString(), ex.Message);
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

            var mockHostClient = new Mock<ISingleClientProxy>();
            var mockJoinerClient = new Mock<ISingleClientProxy>();
            mockClients
                .Setup(clients => clients.Client(connectionId))
                .Returns(mockHostClient.Object);
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
        public async Task StartGame_WithNoHostedGame_ThrowsNoGameToStartException()
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
            Assert.Contains(GameHubErrorCode.NoGameToStart.ToString(), ex.Message);
        }

        [Fact]
        public async Task StartGame_WithNoJoiner_ThrowsNoGameToStartException()
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
            Assert.Contains(GameHubErrorCode.NoJoinerInGame.ToString(), ex.Message);
        }

        [Fact]
        public async Task StartGame_WithAlreadyStartedGame_ThrowsNoGameToStartException()
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
            Assert.Contains(GameHubErrorCode.AlreadyStartedGame.ToString(), ex.Message);
        }
    }
}