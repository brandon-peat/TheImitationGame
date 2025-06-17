import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { IconButton } from '@mui/material';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import './App.css';
import connection from './signalr-connection';


function App() {
  const [mode, setMode] = useState<'select' | 'create' | 'join'>('select');
  const [gameCode, setGameCode] = useState<string>('');
  const [joinResultMessage, setJoinResultMessage] = useState<string>('');

  const changeMode = async (newMode: 'select' | 'create' | 'join') => {
    if (newMode === 'select' && mode === 'create') {
      await connection.invoke('LeaveGame');
      setGameCode('');
    }
    
    if (newMode === 'create') {
      try {
        await connection.invoke<string>('CreateGame').then((code) => {
          setGameCode(code);
        });
      } catch (error) {
        console.error('Error creating game:', error);
      }
    }
    
    setMode(newMode);
  };

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

  useEffect(() => {
    // LATER: turn on listeners here
    
    if (connection.state === 'Disconnected') {
      connection.start();
    }

    return () => {
      // LATER: turn off listeners here
    }
  }, []);

  return (
    <div className='page'>
      <Typography variant='h3' gutterBottom sx={{ textAlign: 'center' }}>
        The Imitation Game
      </Typography>

      {mode === 'select' ? (
        <div className='buttons'>
          <Button
            variant='contained'
            color='primary'
            onClick={() => changeMode('create')}
          >
            Create A Game
          </Button>

          <Button
            variant='contained'
            color='success'
            onClick={() => changeMode('join')}
          >
            Join A Game
          </Button>
        </div>
      ) : (
        <div className='mode-area'>
          <IconButton onClick={() => changeMode('select')}>
            <ArrowBackIcon />
          </IconButton>

          {mode === 'create' && (
            <TextField className='code-input'
              disabled
              label='Share this code with the other player!'
              defaultValue={gameCode}
              variant='filled'
              slotProps={{ inputLabel: {shrink: true }}}
            />
          )}

          {mode === 'join' && (<>
            <TextField className='code-input'
              label='Enter Game Code'
              variant='outlined'
              onKeyDown={(e) => {
                if (e.key === 'Enter')
                  submitCode((e.target as HTMLInputElement).value);
              }}
            />

            <p className="join-result-message"> {joinResultMessage} </p>
          </>)}
        </div>
      )}
    </div>
  );
}

export default App;