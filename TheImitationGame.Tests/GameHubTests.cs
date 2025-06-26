using Xunit;
using Moq;
using Microsoft.AspNetCore.SignalR;
using TheImitationGame.Api.Hubs;
using TheImitationGame.Api.Models;
using System.Collections.Concurrent;

namespace TheImitationGame.Tests
{
    public class GameHubTests
    {
        [Fact]
        public async Task CreateGame_WhenCalled_AddsGameAndAddsCallerToGroupWithCorrectId()
        {
            // Arrange
            var mockClients = new Mock<IHubCallerClients>();
            var mockGroups = new Mock<IGroupManager>();
            var mockContext = new Mock<HubCallerContext>();
            var connectionId = "test-connection-id";

            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            var gamesStore = new InMemoryGamesStore();
            var hub = new GameHub(gamesStore)
            {
                Clients = mockClients.Object,
                Groups = mockGroups.Object,
                Context = mockContext.Object
            };

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
            var mockClients = new Mock<IHubCallerClients>();
            var mockGroups = new Mock<IGroupManager>();
            var mockContext = new Mock<HubCallerContext>();
            var connectionId = "test-connection-id";

            mockContext.Setup(c => c.ConnectionId).Returns(connectionId);

            var gamesStore = new InMemoryGamesStore();
            var hub = new GameHub(gamesStore)
            {
                Clients = mockClients.Object,
                Groups = mockGroups.Object,
                Context = mockContext.Object
            };

            await hub.CreateGame();

            // Act
            async Task<string> act() => await hub.CreateGame();

            // Assert
            var ex = await Assert.ThrowsAsync<GameHubException>(act);
            Assert.Contains(GameHubErrorCode.AlreadyCreatedGame.ToString(), ex.Message);
        }
    }
}