using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;
using TheImitationGame.Api.Interfaces;
using TheImitationGame.Api.Models;

namespace TheImitationGame.Api.Hubs
{
    public class GameHub(IGamesStore games) : Hub
    {
        private readonly IGamesStore Games = games;

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task<string> CreateGame()
        {
            if(GetGameByJoiner(Context.ConnectionId) != null)
                throw new GameHubException(GameHubErrorCode.CreateGame_AlreadyJoinedGame);

            string gameId = Context.ConnectionId;
            var game = new Game(gameId);

            if (!Games.TryAdd(gameId, game))
                throw new GameHubException(GameHubErrorCode.CreateGame_AlreadyCreatedGame);

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
            if (Games.Any(kvp => kvp.Value?.JoinerConnectionId == Context.ConnectionId))
                throw new GameHubException(GameHubErrorCode.JoinGame_AlreadyJoinedGame);

            if (!Games.TryGetValue(gameId, out var game))
                throw new GameHubException(GameHubErrorCode.JoinGame_GameNotFound);

            if (gameId == Context.ConnectionId)
                throw new GameHubException(GameHubErrorCode.JoinGame_CannotJoinOwnGame);

            if (game!.JoinerConnectionId != null)
                throw new GameHubException(GameHubErrorCode.JoinGame_GameFull);

            var joinedGame = game.With(joinerConnectionId: Context.ConnectionId);
            if (!Games.TryUpdate(gameId, joinedGame, game))
                throw new GameHubException(GameHubErrorCode.UnknownError);

            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Client(gameId).SendAsync("GameJoined");
        }

        public async Task StartGame(bool isHostFirst)
        {
            var game = GetGameByHost(Context.ConnectionId)
                ?? throw new GameHubException(GameHubErrorCode.StartGame_NoGameToStart);

            if (game.JoinerConnectionId == null)
                throw new GameHubException(GameHubErrorCode.StartGame_NoJoinerInGame);

            if (game.State != GameState.NotStarted)
                throw new GameHubException(GameHubErrorCode.StartGame_AlreadyStartedGame);

            // TODO: get this from an LLM
            const string defaultPrompt = "A cat exploding";

            var startedGame = game.With(
                state: GameState.Prompting,
                prompt: defaultPrompt,
                prompter: isHostFirst ? Role.Host : Role.Joiner
            );
            if (!Games.TryUpdate(game.HostConnectionId, startedGame, game))
                throw new GameHubException(GameHubErrorCode.UnknownError);

            var prompter = isHostFirst ? game.HostConnectionId : game.JoinerConnectionId;
            var drawer = isHostFirst ? game.JoinerConnectionId : game.HostConnectionId;

            await Clients.Client(prompter!).SendAsync("PromptTimerStarted", defaultPrompt);
            await Clients.Client(drawer!).SendAsync("AwaitPrompt");
        }

        public async Task SubmitPrompt(string prompt)
        {
            
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await CloseGameWithHost(Context.ConnectionId);
            await CloseGameWithJoiner(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        async Task CloseGameWithHost(string host)
        {
            if (Games.TryRemove(host, out Game? game))
            {
                var joiner = game?.JoinerConnectionId;
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
            var joinedGame = Games.FirstOrDefault(kvp => kvp.Value?.JoinerConnectionId == joiner);
            if (!joinedGame.Equals(default(KeyValuePair<string, Game>)))
            {
                string host = joinedGame.Key;
                Games.TryRemove(host, out _);
                await Groups.RemoveFromGroupAsync(host, host);
                await Groups.RemoveFromGroupAsync(joiner, host);
                await Clients.Client(host).SendAsync("JoinerLeft");
            }
        }

        Game? GetGameByHost(string hostId)
        {
            Games.TryGetValue(hostId, out Game? game);
            return game;
        }

        Game? GetGameByJoiner(string joinerId)
        {
            if (joinerId == null)
                return null;

            var game = Games.FirstOrDefault(kvp => kvp.Value?.JoinerConnectionId == joinerId);

            if (game.Equals(default(KeyValuePair<string, Game>)))
                return null;

            return game.Value;
        }

        Game? GetGameByMember(string connectionId)
        {
            var game = GetGameByHost(connectionId) ?? GetGameByJoiner(connectionId);
            return game;
        }

        async Task<List<string>> GenerateImitations(string prompt, string image_b64, int amount)
        {
            var start = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = @"..\TheImitationGame.Image\cli.py",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = start };
            process.Start();

            var input = new
            {
                prompt,
                image_b64,
                amount
            };
            string jsonInput = JsonSerializer.Serialize(input);
            await process.StandardInput.WriteAsync(jsonInput);
            process.StandardInput.Close();

            string output = await process.StandardOutput.ReadToEndAsync();
            string errors = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (process.ExitCode != 0)
                throw new Exception("Python error: " + errors);

            var result = JsonSerializer.Deserialize<List<string>>(output);
            return result!;
        }
    }
}
