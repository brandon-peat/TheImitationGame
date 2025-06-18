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

        UnknownError
    }
}
