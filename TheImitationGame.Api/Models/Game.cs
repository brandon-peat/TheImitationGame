namespace TheImitationGame.Api.Models
{
    public enum GameState
    {
        NotStarted,
        Prompting,
        Drawing,
        Guessing
    }

    public class Game
    {
        public string HostConnectionId { get; set; }
        public string? JoinerConnectionId { get; set; }
        public GameState State { get; set; }
        public string? Prompt { get; set; }

        public Game(string hostConnectionId, string? joinerConnectionId = null, GameState state = GameState.NotStarted, string? prompt = null)
        {
            HostConnectionId = hostConnectionId;
            JoinerConnectionId = joinerConnectionId;
            State = state;
            Prompt = prompt;
        }
    }
}
