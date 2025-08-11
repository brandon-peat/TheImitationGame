import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';

function Draw({connectionReady}: {connectionReady: boolean}) {
  const navigate = useNavigate();

  const [awaitingPrompt, setAwaitingPrompt] = useState(true);
  const [prompt, setPrompt] = useState<string>('');

  useEffect(() => {
    if(!connectionReady) navigate('/');

    const handleDrawTimerStarted = (prompt: string) => {
      setAwaitingPrompt(false);
      setPrompt(prompt);
    }
    connection.on('DrawTimerStarted', handleDrawTimerStarted);

    return () => {
      connection.off('DrawTimerStarted', handleDrawTimerStarted);
    }
  });

  return (
    awaitingPrompt ? (
      <Typography variant='subtitle1' sx={{ textAlign: 'center' }}>
        Your opponent is coming up with a prompt. Get ready to draw!
      </Typography>
    ) : (
      <>
        <div>
          <Typography variant='subtitle2' sx={{ textAlign: 'center', marginTop: 0 }}>
            Your prompt is . . .
          </Typography>

          <Typography variant='h6' sx={{ textAlign: 'center' }}>
            {prompt}
          </Typography>
        </div>

        {/* TODO: Drawable canvas */}
      </>
    )
  );
}

export default Draw;