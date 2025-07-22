namespace TheImitationGame.Api.Models
{
    public class Game
    {
        public string HostConnectionId { get; set; }
        public string? JoinerConnectionId { get; set; }
        public bool HasStarted { get; set; }

        public Game(string hostConnectionId, string? joinerConnectionId = null, bool hasStarted = false)
        {
            HostConnectionId = hostConnectionId;
            JoinerConnectionId = joinerConnectionId;
            HasStarted = hasStarted;
        }
    }
}
