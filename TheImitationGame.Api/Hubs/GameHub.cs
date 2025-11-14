using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;
using TheImitationGame.Api.Interfaces;
using TheImitationGame.Api.Models;
using TheImitationGame.Api.Services;

namespace TheImitationGame.Api.Hubs
{
    public class GameHub(IGamesStore games, IImitationGenerator imitationGenerator, DefaultPromptGenerator defaultPromptGenerator) : Hub
    {
        private readonly IGamesStore Games = games;
        private readonly IImitationGenerator ImitationGenerator = imitationGenerator;
        private readonly DefaultPromptGenerator DefaultPromptGenerator = defaultPromptGenerator;

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

        public async Task StartGame(bool hostIsPrompter)
        {
            var game = GetGameByHost(Context.ConnectionId)
                ?? throw new GameHubException(GameHubErrorCode.StartGame_NoGameToStart);

            if (game.JoinerConnectionId == null)
                throw new GameHubException(GameHubErrorCode.StartGame_NoJoinerInGame);

            if (game.State != GameState.NotStarted)
                throw new GameHubException(GameHubErrorCode.StartGame_AlreadyStartedGame);

            await BeginRound(game, hostIsPrompter);
        }

        public async Task StartNextRound()
        {
            var game = GetGameByHost(Context.ConnectionId)
                ?? throw new GameHubException(GameHubErrorCode.StartNextRound_NoGameToStartNextRound);

            if (game.State != GameState.BetweenRounds)
                throw new GameHubException(GameHubErrorCode.StartNextRound_NotInBetweenRoundsPhase);

            bool hostIsPrompter = (game.Prompter == Role.Host);
            await BeginRound(game, hostIsPrompter);
        }

        async Task BeginRound(Game game, bool hostIsPrompter)
        {
            string defaultPrompt = await DefaultPromptGenerator.GenerateDefaultPromptAsync();
            var updatedGame = game.With(
                state: GameState.Prompting,
                prompt: defaultPrompt,
                prompter: hostIsPrompter ? Role.Host : Role.Joiner
            );

            if (!Games.TryUpdate(game.HostConnectionId, updatedGame, game))
                throw new GameHubException(GameHubErrorCode.UnknownError);

            var prompter = hostIsPrompter ? game.HostConnectionId : game.JoinerConnectionId;
            var drawer = hostIsPrompter ? game.JoinerConnectionId : game.HostConnectionId;

            await Clients.Client(prompter!).SendAsync("PromptTimerStarted", defaultPrompt);
            await Clients.Client(drawer!).SendAsync("AwaitPrompt");
        }

        public async Task SubmitPrompt(string prompt)
        {
            var game = GetGameByMember(Context.ConnectionId)
                ?? throw new GameHubException(GameHubErrorCode.SubmitPrompt_NotInAGame);

            if (game.State != GameState.Prompting)
                throw new GameHubException(GameHubErrorCode.SubmitPrompt_NotInPromptingPhase);

            if (game.Prompter != (Context.ConnectionId == game.HostConnectionId ? Role.Host : Role.Joiner))
                throw new GameHubException(GameHubErrorCode.SubmitPrompt_NotPrompter);

            var updatedGame = game.With(state: GameState.Drawing, prompt: prompt);

            if (!Games.TryUpdate(game.HostConnectionId, updatedGame, game))
                throw new GameHubException(GameHubErrorCode.UnknownError);

            var prompter = (game.Prompter == Role.Host) ? game.HostConnectionId : game.JoinerConnectionId;
            var drawer = (game.Prompter == Role.Host) ? game.JoinerConnectionId : game.HostConnectionId;

            await Clients.Client(prompter!).SendAsync("AwaitDrawings");
            await Clients.Client(drawer!).SendAsync("DrawTimerStarted", prompt);
        }

        public async Task SubmitDrawing(string image_b64)
        {
            var game = GetGameByMember(Context.ConnectionId)
                ?? throw new GameHubException(GameHubErrorCode.SubmitDrawing_NotInAGame);

            if (game.State != GameState.Drawing)
                throw new GameHubException(GameHubErrorCode.SubmitDrawing_NotInDrawingPhase);

            if (game.Prompter == (Context.ConnectionId == game.HostConnectionId ? Role.Host : Role.Joiner))
                throw new GameHubException(GameHubErrorCode.SubmitDrawing_NotDrawer);

            var prompter = (game.Prompter == Role.Host) ? game.HostConnectionId : game.JoinerConnectionId;
            var drawer = (game.Prompter == Role.Host) ? game.JoinerConnectionId : game.HostConnectionId;

            await Clients.Client(drawer!).SendAsync("AwaitImitations");
            await Clients.Client(prompter!).SendAsync("AwaitImitations");

            int imitationsAmount = game.MaximumImages - 1;
            var imitations = await ImitationGenerator.GenerateImitations(game.Prompt!, image_b64, imitationsAmount);

            var imagesRandom = new List<string>(imitations);
            int insertIndex = Random.Shared.Next(0, imitations.Count + 1);
            imagesRandom.Insert(insertIndex, image_b64);

            var updatedGame = game.With(state: GameState.Guessing, realImageIndex: insertIndex);

            if (!Games.TryUpdate(game.HostConnectionId, updatedGame, game))
                throw new GameHubException(GameHubErrorCode.UnknownError);

            await Clients.Client(drawer!).SendAsync("AwaitGuess", imagesRandom, insertIndex);
            await Clients.Client(prompter!).SendAsync("GuessTimerStarted", imagesRandom);
        }

        public async Task SubmitGuess(int guessIndex)
        {
            var game = GetGameByMember(Context.ConnectionId)
                ?? throw new GameHubException(GameHubErrorCode.SubmitGuess_NotInAGame);

            if (game.State != GameState.Guessing)
                throw new GameHubException(GameHubErrorCode.SubmitGuess_NotInGuessingPhase);

            if (game.Prompter != (Context.ConnectionId == game.HostConnectionId ? Role.Host : Role.Joiner))
                throw new GameHubException(GameHubErrorCode.SubmitGuess_NotGuesser);

            if (guessIndex > game.MaximumImages - 1 || guessIndex < 0)
                throw new GameHubException(GameHubErrorCode.SubmitGuess_GuessOutOfRange);

            var prompter = (game.Prompter == Role.Host) ? game.HostConnectionId : game.JoinerConnectionId;
            var drawer = (game.Prompter == Role.Host) ? game.JoinerConnectionId : game.HostConnectionId;

            bool correctGuess = guessIndex == game.RealImageIndex;

            if (correctGuess) // Proceed to the next round when host calls StartNextRound
            {
                Game updatedGame = game.With(
                    state: GameState.BetweenRounds,
                    prompter: (game.Prompter == Role.Host) ? Role.Joiner : Role.Host,
                    maximumImages: game.MaximumImages + 1,
                    roundNumber: game.RoundNumber + 1
                );
                if (!Games.TryUpdate(game.HostConnectionId, updatedGame, game))
                    throw new GameHubException(GameHubErrorCode.UnknownError);

                await Clients.Client(game.HostConnectionId).SendAsync("CorrectGuess-StartBetweenRoundsPhase", updatedGame.RoundNumber);
                await Clients.Client(game.JoinerConnectionId!).SendAsync("CorrectGuess-AwaitNextRoundStart", updatedGame.RoundNumber);
            }
            else // The game ends
            {
                await Clients.Client(prompter!).SendAsync("IncorrectGuess-Lose", game.RealImageIndex);
                await Clients.Client(drawer!).SendAsync("IncorrectGuess-Win", guessIndex);

                if (game.Prompter == Role.Host)
                    await CloseGameWithHost(Context.ConnectionId, notifyJoiner: false);
                else if (game.Prompter == Role.Joiner)
                    await CloseGameWithJoiner(Context.ConnectionId, notifyHost: false);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await CloseGameWithHost(Context.ConnectionId);
            await CloseGameWithJoiner(Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }
        
        async Task CloseGameWithHost(string host, bool notifyJoiner = true)
        {
            if (Games.TryRemove(host, out Game? game))
            {
                var joiner = game?.JoinerConnectionId;
                await Groups.RemoveFromGroupAsync(host, host);
                if (joiner != null)
                {
                    await Groups.RemoveFromGroupAsync(joiner, host);
                    if (notifyJoiner)
                        await Clients.Client(joiner).SendAsync("HostLeft");
                }
            }
        }

        async Task CloseGameWithJoiner(string joiner, bool notifyHost = true)
        {
            var joinedGame = Games.FirstOrDefault(kvp => kvp.Value?.JoinerConnectionId == joiner);
            if (!joinedGame.Equals(default(KeyValuePair<string, Game>)))
            {
                string host = joinedGame.Key;
                Games.TryRemove(host, out _);
                await Groups.RemoveFromGroupAsync(host, host);
                await Groups.RemoveFromGroupAsync(joiner, host);
                if (notifyHost)
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
    }
}
