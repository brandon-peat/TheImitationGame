namespace TheImitationGame.Api.Models
{
    public enum GameHubErrorCode
    {
        CreateGame_AlreadyCreatedGame,
        CreateGame_AlreadyJoinedGame,

        JoinGame_AlreadyJoinedGame,
        JoinGame_CannotJoinOwnGame,
        JoinGame_GameFull,
        JoinGame_GameNotFound,

        StartGame_NoGameToStart,
        StartGame_NoJoinerInGame,
        StartGame_AlreadyStartedGame,

        SubmitPrompt_NotInAGame,
        SubmitPrompt_NotInPromptingPhase,
        SubmitPrompt_NotPrompter,

        SubmitDrawing_NotInAGame,
        SubmitDrawing_NotInDrawingPhase,
        SubmitDrawing_NotDrawer,

        SubmitGuess_NotInAGame,
        SubmitGuess_NotInGuessingPhase,
        SubmitGuess_NotGuesser,
        SubmitGuess_GuessOutOfRange,

        UnknownError
    }
}
