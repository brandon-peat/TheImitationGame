using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

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
                throw new HubException("You have already created a game which has not ended.");

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
                throw new HubException("You are already in a game.");

            if (!Games.TryGetValue(gameId, out var joiner))
                throw new HubException("Game does not exist.");

            if (gameId == Context.ConnectionId)
                throw new HubException("You cannot join your own game.");

            if (joiner != null)
                throw new HubException("Game has already been joined.");

            if (!Games.TryUpdate(gameId, Context.ConnectionId, null))
                throw new HubException("Failed to join the game.");

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
