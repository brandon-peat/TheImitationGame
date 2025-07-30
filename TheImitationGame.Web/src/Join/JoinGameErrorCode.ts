export const JoinGameErrorCodes = {
  AlreadyJoinedGame: 'JoinGame_AlreadyJoinedGame',
  CannotJoinOwnGame: 'JoinGame_CannotJoinOwnGame',
  GameFull: 'JoinGame_GameFull',
  GameNotFound: 'JoinGame_GameNotFound',
  UnknownError: 'UnknownError'
} as const;

export type JoinGameErrorCode = typeof JoinGameErrorCodes[keyof typeof JoinGameErrorCodes];