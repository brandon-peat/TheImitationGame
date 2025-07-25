namespace TheImitationGame.Api.Models
{
    public enum GameHubErrorCode
    {
        // CreateGame errors
        AlreadyCreatedGame,

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
