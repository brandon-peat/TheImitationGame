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
        private readonly Mock<IImitationGenerator> mockImitationGenerator = new();

        private readonly string connectionId = "test-connection-id";
        private readonly string hostConnectionId = "host-connection-id";
        private readonly string joinerConnectionId = "joiner-connection-id";
        private readonly Mock<ISingleClientProxy> mockClient = new();
        private readonly Mock<ISingleClientProxy> mockHostClient = new();
        private readonly Mock<ISingleClientProxy> mockJoinerClient = new();

        private readonly string prompt = "A cat not exploding";
        private readonly string mockB64String = "image";

        public GameHubTests()
        {
            mockContext.Setup(context => context.ConnectionId).Returns(connectionId);

            hub = new GameHub(mockGamesStore.Object, mockImitationGenerator.Object)
            {
                Clients = mockClients.Object,
                Groups = mockGroups.Object,
                Context = mockContext.Object
            };

            mockClients
                .Setup(clients => clients.Client(connectionId))
                .Returns(mockClient.Object);
            mockClients
                .Setup(clients => clients.Client(hostConnectionId))
                .Returns(mockHostClient.Object);
            mockClients
                .Setup(clients => clients.Client(joinerConnectionId))
                .Returns(mockJoinerClient.Object);
        }

        private void SetupGameInStore(Game game)
        {
            mockGamesStore
                .Setup(store => store.TryGetValue(game.HostConnectionId, out It.Ref<Game?>.IsAny))
                .Returns((string k, out Game? g) => { g = game; return true; });

            if (game.JoinerConnectionId != null)
            {
                mockGamesStore
                    .Setup(store => store.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                    .Returns(new KeyValuePair<string, Game>(game.HostConnectionId, game));
            }
        }

        private void SetupTryUpdateCallback(
            string hostConnectionId,
            Action<Game>? onUpdate = null,
            bool returnValue = true)
        {
            mockGamesStore
                .Setup(store => store.TryUpdate(hostConnectionId, It.IsAny<Game>(), It.IsAny<Game>()))
                .Callback((string key, Game newValue, Game _) => onUpdate?.Invoke(newValue))
                .Returns(returnValue);
        }

        private void SetupNoGameWithHostIdInStore(string hostConnectionId)
        {
            mockGamesStore
                .Setup(games => games.TryGetValue(hostConnectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? g) =>
                {
                    g = null;
                    return false;
                });
        }

        private void VerifyClientMessaged(
            string connectionId,
            string methodName,
            int argsLength,
            Func<Times> times)
        {
            mockClients.Verify(
                clients => clients.Client(connectionId).SendCoreAsync(
                    methodName,
                    It.Is<object?[]>(args => args.Length == argsLength),
                    default
                ),
                times
            );
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
            mockGroups.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateGame_HavingJoinedOtherGame_ThrowsWithAlreadyJoinedGameError()
        {
            // Arrange
            Game existingGame = new(hostConnectionId, connectionId);
            SetupGameInStore(existingGame);

            // Act
            async Task<string> act() => await hub.CreateGame();

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.CreateGame_AlreadyJoinedGame.ToString(), ex.Message);
            mockGroups.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LeaveGame_AsHostForEmptyGame_RemovesGameAndRemovesHostFromGroup()
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
            mockGroups.VerifyNoOtherCalls();
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LeaveGame_AsHostForFilledGame_RemovesBothMembersFromGroupAndNotifiesJoiner()
        {
            // Arrange
            mockGamesStore
                .Setup(games => games.TryRemove(connectionId, out It.Ref<Game?>.IsAny))
                .Returns((string key, out Game? game) =>
                {
                    game = new Game(hostConnectionId, joinerConnectionId);
                    return true;
                });

            // Act
            await hub.LeaveGame();

            // Assert
            mockGroups.Verify(
                groups => groups.RemoveFromGroupAsync(connectionId, connectionId, default),
                Times.Once
            );
            mockGroups.Verify(
                groups => groups.RemoveFromGroupAsync(joinerConnectionId, connectionId, default),
                Times.Once
            );
            VerifyClientMessaged(joinerConnectionId, "HostLeft", 0, Times.Once);
            mockGroups.VerifyNoOtherCalls();
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task LeaveGame_AsJoinerForGame_RemovesGameAndRemovesBothMembersFromGroupAndNotifiesHost()
        {
            // Arrange
            Game joinedGame = new(hostConnectionId, connectionId);
            SetupGameInStore(joinedGame);

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
            VerifyClientMessaged(hostConnectionId, "JoinerLeft", 0, Times.Once);
            mockGroups.VerifyNoOtherCalls();
            mockClients.VerifyNoOtherCalls();
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
            Game gameToJoin = new(hostConnectionId);
            SetupGameInStore(gameToJoin);
            Game? updatedGame = null;
            SetupTryUpdateCallback(
                hostConnectionId,
                g => updatedGame = g
            );

            // Act
            await hub.JoinGame(hostConnectionId);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(connectionId, updatedGame.JoinerConnectionId);
            mockGroups.Verify(
                groups => groups.AddToGroupAsync(connectionId, hostConnectionId, default),
                Times.Once
            );
            VerifyClientMessaged(hostConnectionId, "GameJoined", 0, Times.Once);
            mockClients.VerifyNoOtherCalls();
            mockGroups.VerifyNoOtherCalls();
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
            mockClients.VerifyNoOtherCalls();
            mockGroups.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task JoinGame_WithOwnGameCode_ThrowsWithCannotJoinOwnGameError()
        {
            // Arrange
            Game hostedGame = new (connectionId);
            SetupGameInStore(hostedGame);

            // Act
            async Task act() => await hub.JoinGame(connectionId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_CannotJoinOwnGame.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
            mockGroups.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task JoinGame_WhenGameIsFull_ThrowsWithGameFullError()
        {
            // Arrange
            Game fullGame = new(hostConnectionId, joinerConnectionId);
            SetupGameInStore(fullGame);

            // Act
            async Task act() => await hub.JoinGame(hostConnectionId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_GameFull.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
            mockGroups.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("    ")]
        public async Task JoinGame_WithInvalidGameCode_ThrowsWithGameNotFoundError(string? gameId)
        {
            // Arrange
            SetupNoGameWithHostIdInStore(hostConnectionId);

            // Act
            async Task act() => await hub.JoinGame(gameId);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.JoinGame_GameNotFound.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
            mockGroups.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartGame_WithValidJoinedGameAndHostFirst_SetsGameStateToPromptingAndNotifiesPlayers()
        {
            // Arrange
            Game game = new(connectionId, joinerConnectionId);
            SetupGameInStore(game);
            Game? updatedGame = null;
            SetupTryUpdateCallback(
                connectionId,
                g => updatedGame = g
            );

            // Act
            await hub.StartGame(true);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Prompting, updatedGame.State);
            VerifyClientMessaged(connectionId, "PromptTimerStarted", 1, Times.Once);
            VerifyClientMessaged(joinerConnectionId, "AwaitPrompt", 0, Times.Once);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartGame_WithValidJoinedGameAndJoinerFirst_SetsGameStateToPromptingAndNotifiesPlayers()
        {
            // Arrange
            Game game = new(connectionId, joinerConnectionId);
            SetupGameInStore(game);
            Game? updatedGame = null;
            SetupTryUpdateCallback(
                connectionId,
                g => updatedGame = g
            );

            // Act
            await hub.StartGame(false);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Prompting, updatedGame.State);
            VerifyClientMessaged(connectionId, "AwaitPrompt", 0, Times.Once);
            VerifyClientMessaged(joinerConnectionId, "PromptTimerStarted", 1, Times.Once);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartGame_WithNoHostedGame_ThrowsWithNoGameToStartError()
        {
            // Arrange
            SetupNoGameWithHostIdInStore(connectionId);

            // Act
            async Task act() => await hub.StartGame(false);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.StartGame_NoGameToStart.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartGame_WithNoJoiner_ThrowsWithNoJoinerInGameError()
        {
            // Arrange
            Game game = new(connectionId);
            SetupGameInStore(game);

            // Act
            async Task act() => await hub.StartGame(false);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.StartGame_NoJoinerInGame.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task StartGame_WithAlreadyStartedGame_ThrowsWithAlreadyStartedGameError()
        {
            // Arrange
            Game game = new(connectionId, joinerConnectionId, GameState.Prompting);
            SetupGameInStore(game);

            // Act
            async Task act() => await hub.StartGame(false);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.StartGame_AlreadyStartedGame.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitPrompt_WithHostAsPrompter_SetsGameStateToDrawingAndSetsPromptAndNotifiesPlayers()
        {
            // Arrange
            Game game = new(connectionId, joinerConnectionId, GameState.Prompting, prompter: Role.Host);
            SetupGameInStore(game);
            Game? updatedGame = null;
            SetupTryUpdateCallback(
                connectionId,
                g => updatedGame = g
            );

            // Act
            await hub.SubmitPrompt(prompt);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Drawing, updatedGame.State);
            VerifyClientMessaged(connectionId, "AwaitDrawings", 0, Times.Once);
            VerifyClientMessaged(joinerConnectionId, "DrawTimerStarted", 1, Times.Once);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitPrompt_WithJoinerAsPrompter_SetsGameStateToDrawingAndSetsPromptAndNotifiesPlayers()
        {
            // Arrange
            Game game = new(hostConnectionId, connectionId, GameState.Prompting, prompter: Role.Joiner);
            SetupGameInStore(game);
            Game? updatedGame = null;
            SetupTryUpdateCallback(
                hostConnectionId,
                g => updatedGame = g
            );

            // Act
            await hub.SubmitPrompt(prompt);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Drawing, updatedGame.State);
            VerifyClientMessaged(hostConnectionId, "DrawTimerStarted", 1, Times.Once);
            VerifyClientMessaged(connectionId, "AwaitDrawings", 0, Times.Once);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitPrompt_WhenNotInGame_ThrowsWithNotInAGameError()
        {
            // Arrange
            SetupNoGameWithHostIdInStore(connectionId);
            mockGamesStore
                .Setup(games => games.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, Game>, bool>>()))
                .Returns(default(KeyValuePair<string, Game>));

            // Act
            async Task act() => await hub.SubmitPrompt(prompt);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.SubmitPrompt_NotInAGame.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitPrompt_WhenNotInPromptingState_ThrowsWithNotInPromptingPhaseError()
        {
            // Arrange
            Game game = new(connectionId, joinerConnectionId, GameState.NotStarted, prompter: Role.Host);
            SetupGameInStore(game);

            // Act
            async Task act() => await hub.SubmitPrompt(prompt);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.SubmitPrompt_NotInPromptingPhase.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitPrompt_WhenNotPrompter_ThrowsWithNotPrompterError()
        {
            // Arrange
            Game game = new(connectionId, joinerConnectionId, GameState.Prompting, prompter: Role.Joiner);
            SetupGameInStore(game);

            // Act
            async Task act() => await hub.SubmitPrompt(prompt);

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.SubmitPrompt_NotPrompter.ToString(), ex.Message);
            mockClients.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubmitDrawing_WithHostAsDrawer_SetsGameStateToGuessingAndSetsRealImageIndexAndNotifiesPlayers()
        {
            // Arrange
            Game game = new(connectionId, joinerConnectionId, GameState.Drawing, prompt, Role.Joiner);
            SetupGameInStore(game);
            Game? updatedGame = null;
            SetupTryUpdateCallback(
                connectionId,
                g => updatedGame = g
            );

            mockImitationGenerator
                .Setup(gen => gen.GenerateImitations(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(["image1", "image2", "image3"]);

            // Act
            await hub.SubmitDrawing(mockB64String);

            // Assert
            Assert.NotNull(updatedGame);
            Assert.Equal(GameState.Guessing, updatedGame.State);
            Assert.NotNull(updatedGame.RealImageIndex);
            Assert.InRange(updatedGame.RealImageIndex.Value, 0, 3);
            VerifyClientMessaged(connectionId, "AwaitGuess", 0, Times.Once);
            VerifyClientMessaged(joinerConnectionId, "GuessTimerStarted", 1, Times.Once);
            mockClients.VerifyNoOtherCalls();
        }
    }
}