import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { IconButton, TextField } from "@mui/material";
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';
import styles from './Join.module.css';
import { JoinGameErrorCodes } from './JoinGameErrorCode';

function Join() {
  const navigate = useNavigate();

  const [joinResultMessage, setJoinResultMessage] = useState<string>('');
  const [gameJoined, setGameJoined] = useState<boolean>(false);

  const submitGameCode = async (gameCode: string) => {
    if (!gameCode) return;

    connection.invoke('JoinGame', gameCode)
      .then(() => {
        setGameJoined(true);
        setJoinResultMessage('');
      })
      .catch((error: any) => {
        let errorCode: string;
        let errorMessage: string;

        try {
          const errorJson = JSON.parse(error.message.match(/{.*}/)[0]);
          errorCode = errorJson.code;
          errorMessage = errorJson.message;
        } catch {
          setJoinResultMessage('Unknown error occurred.');
          console.error(error.message);
          return;
        }

        if (errorCode === JoinGameErrorCodes.GameNotFound)
          setJoinResultMessage(`Game with code ${gameCode} was not found. Check the code and try again.`);
        else if (errorCode === JoinGameErrorCodes.GameFull)
          setJoinResultMessage(`Game with code ${gameCode} is already full. Try joining a different game.`);

        // these errors should be made impossible by how our UI is set up
        if (
          errorCode === JoinGameErrorCodes.AlreadyJoinedGame ||
          errorCode === JoinGameErrorCodes.CannotJoinOwnGame ||
          errorCode === JoinGameErrorCodes.UnknownError
        ) {
          setJoinResultMessage('Unknown error occurred.');
          console.error(`Error joining game with error code ${errorCode} - ${errorMessage}`);
        }
      });
  }

  return (
    <div className='mode-area'>
      <IconButton onClick={() => navigate('/')}>
        <ArrowBackIcon />
      </IconButton>

      <TextField className='code-input'
        disabled={gameJoined}
        label={gameJoined ? 'Game Joined' : 'Enter Game Code'}
        variant={gameJoined ? 'filled' : 'outlined'}
        onKeyDown={(e) => {
          if (e.key === 'Enter') {
            let input = e.target as HTMLInputElement;
            submitGameCode(input.value);
            input.value = '';
          }
        }}
      />

      <p className={styles['join-result-message']}>
        {joinResultMessage}
      </p>
    </div>
  );
}

export default Join;