namespace TheImitationGame.Api.Models
{
    public enum GameHubErrorCode
    {
        // CreateGame errors
        AlreadyCreatedGame,
        CannotHostWhileJoined,

        // JoinGame errors
        AlreadyJoinedGame,
        CannotJoinOwnGame,
        GameFull,
        GameNotFound,

        // StartGame errors
        NoGameToStart,
        NoJoinerInGame,
        AlreadyStartedGame,

        UnknownError
    }
}
