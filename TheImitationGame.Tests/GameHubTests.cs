using Moq;
using Microsoft.AspNetCore.SignalR;
using TheImitationGame.Api.Hubs;
using TheImitationGame.Api.Models;
using Microsoft.AspNetCore.Connections.Features;

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
            mockGamesStore.Setup(games => games.TryAdd(connectionId, null)).Returns(true);

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
            mockGamesStore.Setup(games => games.TryAdd(connectionId, null)).Returns(false);

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
                .Setup(games => games.TryRemove(connectionId, out It.Ref<string?>.IsAny))
                .Returns((string key, out string? value) => {
                    value = null;
                    return true;
                });

            // Act
            await hub.LeaveGame();

            // Assert
            mockGamesStore.Verify(
                store => store.TryRemove(connectionId, out It.Ref<string?>.IsAny),
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
                .Setup(games => games.TryRemove(connectionId, out It.Ref<string?>.IsAny))
                .Returns((string key, out string? value) => {
                    value = joinerConnectionId;
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
                .Setup(games => games.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, string?>, bool>>()))
                .Returns(new KeyValuePair<string, string?>(hostConnectionId, connectionId));
            mockClients
                .Setup(clients => clients.Client(hostConnectionId))
                .Returns(mockHostClient.Object);

            // Act
            await hub.LeaveGame();

            //Assert
            mockGamesStore.Verify(
                store => store.TryRemove(hostConnectionId, out It.Ref<string?>.IsAny),
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
                .Setup(games => games.TryRemove(connectionId, out It.Ref<string?>.IsAny))
                .Returns(false);
            mockGamesStore
                .Setup(games => games.FirstOrDefault(It.IsAny<Func<KeyValuePair<string, string?>, bool>>()))
                .Returns(default(KeyValuePair<string, string?>));

            // Act
            var exception = await Record.ExceptionAsync(() => hub.LeaveGame());

            // Assert
            Assert.Null(exception);
            mockGamesStore.Verify(
                store => store.TryRemove(connectionId, out It.Ref<string?>.IsAny),
                Times.Once
            );
            mockGroups.VerifyNoOtherCalls();
            mockClients.VerifyNoOtherCalls();
        }
    }
}