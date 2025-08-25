import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import connection from '../signalr-connection';
import Canvas from './Canvas/Canvas';

function Draw() {
  const [awaitingPrompt, setAwaitingPrompt] = useState(true);
  const [awaitingGuess, setAwaitingGuess] = useState(false);
  const [prompt, setPrompt] = useState<string>('');
  const [realImage, setRealImage] = useState<string>('');
  const [imitations, setImitations] = useState<string[]>([]);
  const [submitDisabled, setSubmitDisabled] = useState(false);

  const handleSubmitDrawing = async (imageDataUrl: string) => {
    const rawImageB64 = imageDataUrl.replace(/^data:image\/jpeg;base64,/, '');
    await connection.invoke('SubmitDrawing', rawImageB64)
      .catch((error) => {
        console.error('Error submitting drawing:', error);
      });
  }

  useEffect(() => {
    const handleDrawTimerStarted = (prompt: string) => {
      setAwaitingPrompt(false);
      setPrompt(prompt);
    }
    connection.on('DrawTimerStarted', handleDrawTimerStarted);

    const handleAwaitGuess = (images: string[]) => {
      setAwaitingGuess(true);
      const [real, ...imitations] = images;
      setRealImage(real);
      setImitations(imitations);
    }
    connection.on('AwaitGuess', handleAwaitGuess);

    const handleAwaitImitations = () => {
      setSubmitDisabled(true);
    }
    connection.on('AwaitImitations', handleAwaitImitations);

    return () => {
      connection.off('DrawTimerStarted', handleDrawTimerStarted);
      connection.off('AwaitGuess', handleAwaitGuess);
      connection.off('AwaitImitations', handleAwaitImitations);
    }
  }, []);

  return (
    awaitingPrompt ? (
      <Typography variant='subtitle1' sx={{ textAlign: 'center' }}>
        Your opponent is coming up with a prompt. Get ready to draw!
      </Typography>
    ) : (
      awaitingGuess ? (
        <>
          <Typography variant='subtitle1' sx={{ textAlign: 'center' }}>
            Your opponent has received the following images and is now guessing.
          </Typography>

          <Typography variant='subtitle2' sx={{ textAlign: 'center' }}>
            Your drawing:
          </Typography>

          <img
            src={`data:image/jpeg;base64,${realImage}`}
            className='w-[512px] h-[512px] rounded-2xl shadow-xl' />

          <Typography variant='subtitle2' sx={{ textAlign: 'center' }}>
            AI imitations: 
          </Typography>

          <div className='flex flex-wrap justify-center gap-4'>
            {imitations.map((image) => (
              <img
                src={`data:image/jpeg;base64,${image}`}
                className='w-[512px] h-[512px] rounded-2xl shadow-xl'
              />
            ))}
          </div>
        </>
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

          <Canvas 
            onSubmitDrawing={handleSubmitDrawing}
            submitDisabled={submitDisabled}
          />
        </>
      )
    )
      
  );
}

export default Draw;