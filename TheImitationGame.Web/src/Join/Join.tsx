import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { IconButton, TextField } from "@mui/material";
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';
import styles from './Join.module.css';

function Join() {
  const navigate = useNavigate();

  const [joinResultMessage, setJoinResultMessage] = useState<string>('');

  const submitCode = async (code: string) => {
    if (!code) return;

    try {
      await connection.invoke('JoinGame', code).then(() => {
        setJoinResultMessage(`Game ${code} joined!`);
      });
    }
    catch (error: any) {
      const message = error?.message || error;
      if (message.includes('Game does not exist')) {
        setJoinResultMessage(`Game ${code} does not exist.`);
      }
      else if (message.includes('Game has already been joined')) {
        setJoinResultMessage(`Game ${code} is full.`);
      }
      else {
        console.error('Unknown error joining game:', error);
        setJoinResultMessage(`Unknown error joining game ${code}.`);
      }
    }
  }

  return (
    <div className={styles['mode-area']}>
      <IconButton onClick={() => navigate('/')}>
        <ArrowBackIcon />
      </IconButton>

      <TextField className={styles['code-input']}
        label='Enter Game Code'
        variant='outlined'
        onKeyDown={(e) => {
          if (e.key === 'Enter')
            submitCode((e.target as HTMLInputElement).value);
          }}
      />

      <p className={styles['join-result-message']}>
        {joinResultMessage}
      </p>
    </div>
  );
}

export default Join;