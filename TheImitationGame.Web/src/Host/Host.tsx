import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { IconButton, TextField } from "@mui/material";
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';

function Host({connectionReady}: {connectionReady: boolean}) {
  const [gameCode, setGameCode] = useState<string>('');
  
  const navigate = useNavigate();

  useEffect(() => {
    if (!connectionReady) return;

    const createGame = async () => {
        await connection.invoke<string>('CreateGame')
          .then((code) => setGameCode(code))
          .catch((error) => console.error('Error creating game:', error));
    };

    createGame();

    return () => {
      connection.invoke('LeaveGame')
        .catch((error) => {
          console.error('Error leaving game:', error);
        });
    }
  }, [connectionReady]);

  return (
    <div className='mode-area'>
      <IconButton onClick={() => navigate('/')}>
        <ArrowBackIcon />
      </IconButton>

      <TextField className='code-input'
        disabled
        label='Share this code with the other player!'
        defaultValue= {gameCode}
        variant='filled'
        slotProps={{ inputLabel: {shrink: true }}}
      />
    </div>
  );
}

export default Host;