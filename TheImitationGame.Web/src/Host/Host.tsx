import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { IconButton, TextField } from "@mui/material";
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';
import styles from './Host.module.css';

function Host() {
  const [gameCode, setGameCode] = useState<string>('');
  
  const navigate = useNavigate();

  useEffect(() => {
    const createGame = async () => {
      try {
        await connection.invoke<string>('CreateGame').then((code) => {
          setGameCode(code);
        });
      } catch (error) {
        console.error('Error creating game:', error);
      }
    };

    createGame();

    return () => {
      connection.invoke('LeaveGame').catch((error) => {
        console.error('Error leaving game:', error);
      });
    }
  }, []);

  return (
    <div className={styles['mode-area']}>
      <IconButton onClick={() => navigate('/')}>
        <ArrowBackIcon />
      </IconButton>

      <TextField className={styles['code-input']}
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