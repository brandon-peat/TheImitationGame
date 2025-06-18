using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using TheImitationGame.Api.Models;

namespace TheImitationGame.Api.Hubs
{
    public class GameHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string?> Games = new();

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task<string> CreateGame()
        {
            string gameId = Context.ConnectionId;
            if (!Games.TryAdd(gameId, null))
                throw new GameHubException(GameHubErrorCode.AlreadyCreatedGame);

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            return gameId;
        }

        public async Task LeaveGame()
        {
            await CloseGameWithHost(Context.ConnectionId);
            await CloseGameWithJoiner(Context.ConnectionId);
        }

        public async Task JoinGame(string gameId)
        {
            if (Games.Any(kvp => kvp.Value == Context.ConnectionId))
                throw new GameHubException(GameHubErrorCode.AlreadyJoinedGame);

            if (!Games.TryGetValue(gameId, out var joiner))
                throw new GameHubException(GameHubErrorCode.GameNotFound);

            if (gameId == Context.ConnectionId)
                throw new GameHubException(GameHubErrorCode.CannotJoinOwnGame);

            if (joiner != null)
                throw new GameHubException(GameHubErrorCode.GameFull);

            if (!Games.TryUpdate(gameId, Context.ConnectionId, null))
                throw new GameHubException(GameHubErrorCode.UnknownError);

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await CloseGameWithHost(Context.ConnectionId);
            await CloseGameWithJoiner(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        async Task CloseGameWithHost(string host)
        {
            if (Games.TryRemove(host, out string? joiner))
            {
                await Groups.RemoveFromGroupAsync(host, host);
                if (joiner != null)
                {
                    await Groups.RemoveFromGroupAsync(joiner, host);
                    await Clients.Client(joiner).SendAsync("HostLeft");
                }
            }
        }

        async Task CloseGameWithJoiner(string joiner)
        {
            var joinedGame = Games.FirstOrDefault(kvp => kvp.Value == joiner);
            if (!joinedGame.Equals(default(KeyValuePair<string, string?>)))
            {
                string host = joinedGame.Key;
                Games.TryRemove(host, out _);
                await Groups.RemoveFromGroupAsync(host, host);
                await Groups.RemoveFromGroupAsync(joiner, host);
                await Clients.Client(host).SendAsync("JoinerLeft");
            }
        }
    }
}
