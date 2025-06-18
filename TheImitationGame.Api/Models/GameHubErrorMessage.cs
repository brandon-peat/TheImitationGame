namespace TheImitationGame.Api.Models
{
    public static class GameHubErrorMessage
    {
        public static string GetMessage(GameHubErrorCode code) => code switch
        {
            // CreateGame errors
            GameHubErrorCode.AlreadyCreatedGame => "You have already created a game which has not ended.",

            // JoinGame errors
            GameHubErrorCode.AlreadyJoinedGame => "You are already in a game.",
            GameHubErrorCode.CannotJoinOwnGame => "You cannot join your own game.",
            GameHubErrorCode.GameFull => "Game has already been joined.",
            GameHubErrorCode.GameNotFound => "Game was not found.",

            GameHubErrorCode.UnknownError => "An unknown error occurred.",
            _ => "An unknown error occurred."
        };
    }
}
