namespace TheImitationGame.Api.Models
{
    public enum GameState
    {
        NotStarted,
        Prompting,
        Drawing,
        Guessing
    }

    public enum Role
    {
        Host,
        Joiner
    }

    public class Game
    {
        public string HostConnectionId { get; set; }
        public string? JoinerConnectionId { get; set; }
        public GameState State { get; set; }
        public string? Prompt { get; set; }
        public Role? Prompter { get; set; }

        public Game(
            string hostConnectionId,
            string? joinerConnectionId = null,
            GameState state = GameState.NotStarted,
            string? prompt = null,
            Role? prompter = null)
        {
            HostConnectionId = hostConnectionId;
            JoinerConnectionId = joinerConnectionId;
            State = state;
            Prompt = prompt;
            Prompter = prompter;
        }

        public Game With(
            string? hostConnectionId = null,
            string? joinerConnectionId = null,
            GameState? state = null,
            string? prompt = null,
            Role? prompter = null)
        {
            return new Game(
                hostConnectionId ?? HostConnectionId,
                joinerConnectionId ?? JoinerConnectionId,
                state ?? State,
                prompt ?? Prompt,
                prompter ?? Prompter
            );
        }
    }
}
