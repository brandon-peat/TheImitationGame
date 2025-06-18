export const JoinGameErrorCodes = {
  AlreadyJoinedGame: 'AlreadyJoinedGame',
  CannotJoinOwnGame: 'CannotJoinOwnGame',
  GameFull: 'GameFull',
  GameNotFound: 'GameNotFound',
  UnknownError: 'UnknownError'
} as const;

export type JoinGameErrorCode = typeof JoinGameErrorCodes[keyof typeof JoinGameErrorCodes];