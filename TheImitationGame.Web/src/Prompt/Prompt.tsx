import { TextField } from '@mui/material';
import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';

function Prompt({connectionReady}: {connectionReady: boolean}) {
  const navigate = useNavigate();
  const location = useLocation();
  const defaultPrompt = location.state?.defaultPrompt;

  const [prompt, setPrompt] = useState<string>(defaultPrompt);
  const [promptSent, setPromptSent] = useState(false);

  

  useEffect(() => {
    if(!connectionReady || !defaultPrompt) navigate('/');

    const handleAwaitDrawings = () => {
      setPromptSent(true);
    };
    connection.on('AwaitDrawings', handleAwaitDrawings);

    const handleGuessTimerStarted = (images: string[]) => {
      navigate('/guess', {state: {images}});
    }
    connection.on('GuessTimerStarted', handleGuessTimerStarted);

    return () => {
      connection.off('AwaitDrawings', handleAwaitDrawings);
      connection.off('GuessTimerStarted', handleGuessTimerStarted);
    }
  }, []);

  return (
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
  );
}

export default Prompt;