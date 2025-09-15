import { TextField } from '@mui/material';
import { useEffect, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';
import Timer from '../Timer/Timer';

function Prompt() {
  const timerDurationSeconds: number = 30;

  const navigate = useNavigate();
  const location = useLocation();
  const defaultPrompt: string = location.state?.defaultPrompt;

  const [prompt, setPrompt] = useState<string>(defaultPrompt);
  const [promptSent, setPromptSent] = useState(false);

  useEffect(() => {
    if(!defaultPrompt) navigate('/');

    const handleAwaitDrawings = () => {
      setPromptSent(true);
    };
    connection.on('AwaitDrawings', handleAwaitDrawings);

    const handleAwaitImitations = () => {
      navigate('/guess');
    }
    connection.on('AwaitImitations', handleAwaitImitations);

    return () => {
      connection.off('AwaitDrawings', handleAwaitDrawings);
      connection.off('AwaitImitations', handleAwaitImitations);
    }
  }, []);

  return (
    <>
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
              .catch((error) => {
                console.error('Error submitting prompt:', error);
              });
          }
        }}
      />

      {!promptSent &&
        <div className='absolute top-4 right-4'>
          <Timer
            durationSeconds={timerDurationSeconds}
            onTimeout={() => {
              const finalPrompt = prompt.trim() || defaultPrompt.trim();

              connection.invoke('SubmitPrompt', finalPrompt)
                .catch((error) => {
                  console.error('Error submitting prompt on timeout:', error);
                });
            }}
          />
        </div>
      }
    </>
  );
}

export default Prompt;