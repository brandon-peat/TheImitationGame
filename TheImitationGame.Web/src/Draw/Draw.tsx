import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';
import Canvas from './Canvas/Canvas';

function Draw({connectionReady}: {connectionReady: boolean}) {
  const navigate = useNavigate();

  const [awaitingPrompt, setAwaitingPrompt] = useState(true);
  const [awaitingGuess, setAwaitingGuess] = useState(false);
  const [prompt, setPrompt] = useState<string>('');

  const handleSubmitDrawing = async (imageDataUrl: string) => {
    const rawImageB64 = imageDataUrl.replace(/^data:image\/jpeg;base64,/, '');
    await connection.invoke('SubmitDrawing', rawImageB64)
    .catch((error) => {
      console.error('Error submitting drawing:', error);
    });
  }

  useEffect(() => {
    if(!connectionReady) navigate('/');

    const handleDrawTimerStarted = (prompt: string) => {
      setAwaitingPrompt(false);
      setPrompt(prompt);
    }
    connection.on('DrawTimerStarted', handleDrawTimerStarted);

    const handleAwaitGuess = () => {
      setAwaitingGuess(true);
    }
    connection.on('AwaitGuess', handleAwaitGuess);

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
      awaitingGuess ? (
        <Typography variant='subtitle1' sx={{ textAlign: 'center' }}>
          Your opponent has received your drawing along with the AI imitations and is now guessing.
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

          <Canvas onSubmitDrawing={handleSubmitDrawing} />
        </>
      )
    )
      
  );
}

export default Draw;