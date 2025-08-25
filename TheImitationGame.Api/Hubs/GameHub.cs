using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text.Json;
using TheImitationGame.Api.Interfaces;
using TheImitationGame.Api.Models;

namespace TheImitationGame.Api.Hubs
{
    public class GameHub(IGamesStore games, IImitationGenerator imitationGenerator) : Hub
    {
        private readonly IGamesStore Games = games;
        private readonly IImitationGenerator ImitationGenerator = imitationGenerator;

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
                prompter: isHostFirst ? Role.Host : Role.Joiner);

            if (!Games.TryUpdate(game.HostConnectionId, startedGame, game))
                throw new GameHubException(GameHubErrorCode.UnknownError);

            var prompter = isHostFirst ? game.HostConnectionId : game.JoinerConnectionId;
            var drawer = isHostFirst ? game.JoinerConnectionId : game.HostConnectionId;

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

            // TODO: increment each round
            int imitationsAmount = game.MaximumImages - 1;
            var imitations = await ImitationGenerator.GenerateImitations(game.Prompt!, image_b64, imitationsAmount);

            var imagesRandom = new List<string>(imitations);
            int insertIndex = Random.Shared.Next(0, imitations.Count + 1);
            imagesRandom.Insert(insertIndex, image_b64);

            var imagesWithRealFirst = new List<string>(imitations);
            imagesWithRealFirst.Insert(0, image_b64);

            var updatedGame = game.With(state: GameState.Guessing, realImageIndex: insertIndex);

            if (!Games.TryUpdate(game.HostConnectionId, updatedGame, game))
                throw new GameHubException(GameHubErrorCode.UnknownError);

            await Clients.Client(drawer!).SendAsync("AwaitGuess", imagesWithRealFirst);
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

            if (correctGuess) // Continue to the next round with swapped roles and more fakes
            {
                Game updatedGame = game.With(
                    state: GameState.BetweenRounds,
                    prompt: null,
                    prompter: (game.Prompter == Role.Host) ? Role.Joiner : Role.Host,
                    maximumImages: game.MaximumImages + 1,
                    realImageIndex: null
                );

                if (!Games.TryUpdate(game.HostConnectionId, updatedGame, game))
                    throw new GameHubException(GameHubErrorCode.UnknownError);

                await Clients.Client(game.HostConnectionId).SendAsync("CorrectGuess-StartBetweenRoundsPhase");
                await Clients.Client(game.JoinerConnectionId!).SendAsync("CorrectGuess-AwaitNextRoundStart");

            }
            else // The game ends
            {
                await Clients.Client(prompter!).SendAsync("IncorrectGuess-Lose");
                await Clients.Client(drawer!).SendAsync("IncorrectGuess-Win", guessIndex);

                await CloseGameWithHost(Context.ConnectionId);
                await CloseGameWithJoiner(Context.ConnectionId);
            }
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
    }
}
