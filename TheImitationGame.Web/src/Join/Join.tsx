import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { IconButton, TextField } from "@mui/material";
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';
import { JoinGameErrorCodes } from './JoinGameErrorCode';

function Join({connectionReady}: {connectionReady: boolean}) {
  const navigate = useNavigate();

  const [joinResultMessage, setJoinResultMessage] = useState<string>('');
  const [gameJoined, setGameJoined] = useState<boolean>(false);

  useEffect(() => {
    if (!connectionReady) return;

    const handleHostLeft = () => {
      setGameJoined(false);
      setJoinResultMessage('Host has left the game.');
    };
    connection.on('HostLeft', handleHostLeft);

    return () => {
      connection.invoke('LeaveGame')
        .catch((error) => {
          console.error('Error leaving game:', error);
        });
    }
  }, [connectionReady]);

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

        // these errors should be made impossible by the UI
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
      <IconButton onClick={() => {
          navigate('/');
          if (gameJoined) {
            connection.invoke('LeaveGame')
              .catch((error) => {
                console.error('Error leaving game:', error);
              });
          }
      }}>
        <ArrowBackIcon />
      </IconButton>

      <TextField className='code-input'
        disabled={gameJoined}
        label={gameJoined ? 'Waiting for host to start . . .' : 'Enter Game Code'}
        variant={gameJoined ? 'filled' : 'outlined'}
        onKeyDown={(e) => {
          if (e.key === 'Enter') {
            let input = e.target as HTMLInputElement;
            submitGameCode(input.value);
            input.value = '';
          }
        }}
      />

      <div className='flex-break' />

      <p>
        {joinResultMessage}
      </p>
    </div>
  );
}

export default Join;