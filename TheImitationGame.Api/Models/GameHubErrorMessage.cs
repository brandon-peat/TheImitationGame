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

            // StartGame errors
            GameHubErrorCode.NoGameToStart => "You are not hosting a game.",
            GameHubErrorCode.NoJoinerInGame => "Someone has to join your game before it can be started.",
            GameHubErrorCode.AlreadyStartedGame => "Your game has already been started.",

            GameHubErrorCode.UnknownError => "An unknown error occurred.",
            _ => "An unknown error occurred."
        };
    }
}
