import { TextField, Typography } from '@mui/material';
import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';

function Prompt({connectionReady}: {connectionReady: boolean}) {
  const navigate = useNavigate();
  const location = useLocation();
  const defaultPrompt = location.state?.defaultPrompt;

  const [prompt, setPrompt] = useState<string>(defaultPrompt);
  const [prompting, setPrompting] = useState(true);
  const [promptSent, setPromptSent] = useState(false);
  const [images, setImages] = useState<string[]>([]);

  useEffect(() => {
    if(!connectionReady || !defaultPrompt) navigate('/');

    const handleAwaitDrawings = () => {
      setPromptSent(true);
    };
    connection.on('AwaitDrawings', handleAwaitDrawings);

    const handleGuessTimerStarted = (images: string[]) => {
      setPrompting(false);
      setImages(images);
    }
    connection.on('GuessTimerStarted', handleGuessTimerStarted);

    return () => {
      connection.off('AwaitDrawings', handleAwaitDrawings);
    }
  });

  return (
    prompting ? (
      <TextField sx={{ width: '60%' }}
        label={promptSent ? 'Your opponent is now drawing based on your prompt.' : 'What should your opponent draw?'}
        disabled={promptSent}
        defaultValue={prompt}
        variant={promptSent ? 'filled' : 'outlined'}
        onKeyDown={(e) => {
          if (e.key === 'Enter') {
            let input = e.target as HTMLInputElement;
            let prompt = input.value.trim();
            if (!prompt) return;
            
            setPrompt(prompt);
            connection.invoke('SubmitPrompt', prompt)
              .then(() => {
                input.value = '';
              })
              .catch((error) => {
                console.error('Error submitting prompt:', error);
              });
          }
        }}
      />
    ) : (
      <>
        <Typography variant='subtitle1' sx={{ textAlign: 'center' }}>
          One of these is the real drawing, and the rest are AI imitations.
          Select the one you think is real and submit your guess.
        </Typography>

        <div className='flex flex-wrap justify-center gap-4 pb-10'>
          {images.map((image) => (
            <img
              src={`data:image/jpeg;base64,${image}`}
              className='w-[512px] h-[512px] rounded-2xl hover:shadow-2xl transition duration-300'
            />
          ))}
        </div>
      </>
    )
  );
}

export default Prompt;