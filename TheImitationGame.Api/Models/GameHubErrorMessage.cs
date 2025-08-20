namespace TheImitationGame.Api.Models
{
    public static class GameHubErrorMessage
    {
        public static string GetMessage(GameHubErrorCode code) => code switch
        {
            GameHubErrorCode.CreateGame_AlreadyCreatedGame => "You have already created a game which has not ended.",
            GameHubErrorCode.CreateGame_AlreadyJoinedGame => "You cannot host a game while having joined someone else's.",

            GameHubErrorCode.JoinGame_AlreadyJoinedGame => "You are already in a game.",
            GameHubErrorCode.JoinGame_CannotJoinOwnGame => "You cannot join your own game.",
            GameHubErrorCode.JoinGame_GameFull => "This game has already been joined.",
            GameHubErrorCode.JoinGame_GameNotFound => "Game was not found.",

            GameHubErrorCode.StartGame_NoGameToStart => "You are not hosting a game.",
            GameHubErrorCode.StartGame_NoJoinerInGame => "You cannot start a game which is empty.",
            GameHubErrorCode.StartGame_AlreadyStartedGame => "Your game has already been started.",

            GameHubErrorCode.SubmitPrompt_NotInAGame => "You are not in a game.",
            GameHubErrorCode.SubmitPrompt_NotInPromptingPhase => "This game is not in the prompting phase.",
            GameHubErrorCode.SubmitPrompt_NotPrompter => "You are not currently the prompter for this game.",

            GameHubErrorCode.SubmitDrawing_NotInAGame => "You are not in a game.",
            GameHubErrorCode.SubmitDrawing_NotInDrawingPhase => "This game is not in the drawing phase.",
            GameHubErrorCode.SubmitDrawing_NotDrawer => "You are not currently the drawer for this game.",

            GameHubErrorCode.SubmitGuess_NotInAGame => "You are not in a game.",
            GameHubErrorCode.SubmitGuess_NotInGuessingPhase => "This is not in the guessing phase.",
            GameHubErrorCode.SubmitGuess_NotGuesser => "You are not currently the prompter (guesser) for this game.",
            GameHubErrorCode.SubmitGuess_GuessOutOfRange => "That value is not in a valid range.",

            GameHubErrorCode.UnknownError => "An unknown error occurred.",
            _ => "An unknown error occurred."
        };
    }
}
