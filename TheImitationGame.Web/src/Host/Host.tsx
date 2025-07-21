import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import { Button, FormControl, IconButton, TextField } from "@mui/material";
import FormControlLabel from '@mui/material/FormControlLabel';
import FormLabel from '@mui/material/FormLabel';
import Radio from '@mui/material/Radio';
import RadioGroup from '@mui/material/RadioGroup';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';

function Host({connectionReady}: {connectionReady: boolean}) {
  const [gameCode, setGameCode] = useState<string>('');
  const [gameJoined, setGameJoined] = useState<boolean>(false);
  const [firstPlayer, setFirstPlayer] = useState<'Me' | 'Opponent' | 'Random'>('Me');

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
        <>
          <FormControl>
            <FormLabel> Who should give the first prompt? </FormLabel>
            <RadioGroup
              row
              value={firstPlayer}
              onChange={(e) => setFirstPlayer(e.target.value as 'Me' | 'Opponent' | 'Random')}
            >
              <FormControlLabel value='Me' control={<Radio />} label='Me' />
              <FormControlLabel value='Opponent' control={<Radio />} label='Opponent' />
              <FormControlLabel value='Random' control={<Radio />} label='Random' />
            </RadioGroup>
          </FormControl>

          <div className='flex-break' />

          <Button
            variant='contained'
            color='primary'
            onClick={() => {
              const isHostFirst = 
                firstPlayer === 'Me' ||
                (firstPlayer === 'Random' && Math.random() < 0.5);
              connection.invoke('StartGame', isHostFirst)
                .catch((error) => {
                  console.error('Error starting game:', error);
                });
            }}
          >
            Start Game
          </Button>
        </>
      )}
    </div>
  );
}

export default Host;