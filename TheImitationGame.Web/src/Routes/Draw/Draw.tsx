import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../../Utilities/signalr-connection';
import Canvas from './Canvas/Canvas';

function Draw() {
  const navigate = useNavigate();

  const [awaitingPrompt, setAwaitingPrompt] = useState(true);
  const [awaitingGuess, setAwaitingGuess] = useState(false);
  const [prompt, setPrompt] = useState<string>('');
  const [realImage, setRealImage] = useState<string>('');
  const [realImageIndex, setRealImageIndex] = useState<number>(-1);
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

    const handleAwaitGuess = (images: string[], realImageIndex: number) => {
      setAwaitingGuess(true);
      setRealImageIndex(realImageIndex);
      setRealImage(images[realImageIndex]);
      setImitations(images);
    }
    connection.on('AwaitGuess', handleAwaitGuess);

    const handleAwaitImitations = () => {
      setSubmitDisabled(true);
    }
    connection.on('AwaitImitations', handleAwaitImitations);

    const handleCorrectGuessAsHost = (roundNumber: number) => {
      navigate('/next-round', { state: { roundNumber: roundNumber, role: 'drawer', isHost: true } });
    }
    connection.on('CorrectGuess-StartBetweenRoundsPhase', handleCorrectGuessAsHost);

    const handleCorrectGuessAsJoiner = (roundNumber: number) => {
      navigate('/next-round', { state: { roundNumber: roundNumber, role: 'drawer', isHost: false } });
    }
    connection.on('CorrectGuess-AwaitNextRoundStart', handleCorrectGuessAsJoiner);

    return () => {
      connection.off('DrawTimerStarted', handleDrawTimerStarted);
      connection.off('AwaitGuess', handleAwaitGuess);
      connection.off('AwaitImitations', handleAwaitImitations);
      connection.off('CorrectGuess-AwaitNextRoundStart', handleCorrectGuessAsHost);
      connection.on('CorrectGuess-AwaitNextRoundStart', handleCorrectGuessAsJoiner);
    }
  }, []);

  useEffect(() => {
    const handleWin = (guessIndex: number) => {
      var wrongImage = imitations[guessIndex];
      navigate('/end', {state: {won: true, wrongImage, realImage}});
    }
    connection.on('IncorrectGuess-Win', handleWin);

    return () => {
      connection.off('IncorrectGuess-Win', handleWin);
    }
  }, [imitations, realImage]);

  return (
    awaitingPrompt ? (
      <Typography variant='subtitle1'>
        Your opponent is coming up with a prompt. Get ready to draw!
      </Typography>
    ) : (
      awaitingGuess ? (
        <>
          <Typography variant='subtitle1'>
            Your opponent has received the following images and is now guessing.
          </Typography>

          <Typography variant='subtitle2'>
            Your drawing:
          </Typography>

          <img
            src={`data:image/jpeg;base64,${realImage}`}
            className='w-[512px] h-[512px] rounded-2xl shadow-xl' />

          <Typography variant='subtitle2'>
            AI imitations: 
          </Typography>

          <div className='flex flex-wrap justify-center gap-4'>
            {imitations.map((image, i) => (
              i === realImageIndex ? null : (
                <img
                  key={i}
                  src={`data:image/jpeg;base64,${image}`}
                  className='w-[512px] h-[512px] rounded-2xl shadow-xl'
                />
              )
            ))}
          </div>
        </>
      ) : (
        <>
          <div>
            <Typography variant='subtitle2'>
              Your prompt is . . .
            </Typography>

            <Typography variant='h6'>
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