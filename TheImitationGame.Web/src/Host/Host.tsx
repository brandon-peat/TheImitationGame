import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { Button, IconButton, TextField } from "@mui/material";
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';

function Host({connectionReady}: {connectionReady: boolean}) {
  const [gameCode, setGameCode] = useState<string>('');
  const [gameJoined, setGameJoined] = useState<boolean>(false);
  
  const navigate = useNavigate();

  useEffect(() => {
    if (!connectionReady) return;

    const createGame = async () => {
      connection.invoke<string>('CreateGame')
        .then((code) => setGameCode(code))
        .catch((error) => console.error('Error creating game:', error));
    };

    createGame();

    const handleGameJoined = () => {
      setGameJoined(true);
    };
    connection.on('GameJoined', handleGameJoined);

    const handleJoinerLeft = () => {
      setGameJoined(false);
      createGame();
    };
    connection.on('JoinerLeft', handleJoinerLeft);

    return () => {
      connection.off('GameJoined', handleGameJoined);
      
      connection.invoke('LeaveGame')
        .catch((error) => {
          console.error('Error leaving game:', error);
        });
    }
  }, [connectionReady]);

  return (
    <div className='mode-area'>
      <IconButton onClick={() => {
          navigate('/');
          connection.invoke('LeaveGame')
            .catch((error) => {
              console.error('Error leaving game:', error);
            });
        }}>
        <ArrowBackIcon />
      </IconButton>

      <TextField className='code-input'
        disabled
        label={gameJoined ? 'Game Joined!' : 'Share this code with the other player!'}
        defaultValue= {gameCode}
        variant='filled'
        slotProps={{ inputLabel: {shrink: true }}}
      />

      <div className='flex-break' />

      {gameJoined && (
        <Button
          variant='contained'
          color='primary'
          onClick={() => {
            connection.invoke('StartGame')
              .catch((error) => {
                console.error('Error starting game:', error);
              });
          }}
        >
          Start Game
        </Button>
      )}
    </div>
  );
}

export default Host;