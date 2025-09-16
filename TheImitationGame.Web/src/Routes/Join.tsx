import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import ArrowForwardIcon from '@mui/icons-material/ArrowForward';
import { IconButton, InputAdornment, TextField, Typography } from "@mui/material";
import Alert from '@mui/material/Alert';
import Snackbar from '@mui/material/Snackbar';
import { motion } from 'framer-motion';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../Utilities/signalr-connection';
import ScrollingEllipsis from '../Utilities/ScrollingEllipsis';
import WaitingSpinner from '../Utilities/WaitingSpinner';

function Join() {
  const [gameCodeInput, setGameCodeInput] = useState("");
  const [snackbarMessage, setSnackbarMessage] = useState<string>('');
  const [errorOpen, setErrorOpen] = useState(false);
  const [gameJoined, setGameJoined] = useState<boolean>(false);
  const [inputShake, setInputShake] = useState(false);

  const navigate = useNavigate();

  useEffect(() => {
    const handleGameStartedAsPrompter = (defaultPrompt: string) => {
      navigate('/prompt', { state: { defaultPrompt } });
    }
    connection.on('PromptTimerStarted', handleGameStartedAsPrompter);

    const handleGameStartedAsDrawer = () => {
      navigate('/draw');
    }
    connection.on('AwaitPrompt', handleGameStartedAsDrawer);

    return () => {
      connection.off('PromptTimerStarted', handleGameStartedAsPrompter);
      connection.off('AwaitPrompt', handleGameStartedAsDrawer);
    }
  }, []);

  const submitGameCode = async (gameCode: string) => {
    if (!gameCode) return;

    connection.invoke('JoinGame', gameCode)
      .then(() => {
        setGameJoined(true);
        setErrorOpen(false);
        setSnackbarMessage('');
      })
      .catch((error: any) => {
        setErrorOpen(true);
        setInputShake(true);
        setTimeout(() => setInputShake(false), 500);

        try {
          const errorJson = JSON.parse(error.message.match(/{.*}/)[0]);
          const errorCode = errorJson.code;

          if (errorCode === 'JoinGame_GameNotFound') {
            setSnackbarMessage(`Game with code ${gameCode} was not found. Double check the code and try again.`);
          } else if (errorCode === 'JoinGame_GameFull') {
            setSnackbarMessage(`Game with code ${gameCode} is already full. Try joining a different game.`);
          } else {
            setSnackbarMessage('Unknown error occurred.');
            console.error(`Unexpected error code: ${errorCode}`, errorJson);
          }
        } catch {
          setSnackbarMessage('Unknown error occurred.');
          console.error(error);
        }
      });
  }

  return (
    <>
      <div className='flex flex-wrap gap-[0.5rem]'>
        <IconButton onClick={() => {
            navigate('/');
            if (gameJoined) {
              connection.invoke('LeaveGame')
                .catch((error) => {
                  console.error('Error leaving game:', error);
                });
            }
        }}>
          <ArrowBackIcon />
        </IconButton>

        <motion.div
          animate={inputShake ? { x: [0, -5, 5, -5, 5, 0] } : {}}
          transition={{ duration: 0.4, ease: 'easeInOut' }}
        >
          <TextField
            className='w-xs'
            autoFocus
            error={inputShake}
            disabled={gameJoined}
            label={gameJoined ? gameCodeInput : 'Enter Game Code'}
            variant={gameJoined ? 'filled' : 'outlined'}
            value={gameJoined ? '' : gameCodeInput}
            onChange={(e) => setGameCodeInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && gameCodeInput) {
                submitGameCode(gameCodeInput.trim());
              }
            }}
            slotProps={{
              input: {
                autoComplete: 'off',
                endAdornment: !gameJoined && (
                  <InputAdornment position='end'>
                    <IconButton
                      edge='end'
                      onMouseDown={(e) => e.preventDefault()} // So the input stays focused
                      onClick={() => {
                        if (gameCodeInput)
                          submitGameCode(gameCodeInput.trim());
                      }}
                    >
                      <ArrowForwardIcon />
                    </IconButton>
                  </InputAdornment>
                ),
              },
            }}
          />
        </motion.div>
      </div>

      {gameJoined && (
        <div className='flex items-center justify-center gap-2'>
          <WaitingSpinner />

          <Typography className='text-gray-700'>
            Waiting for the host to start the game <ScrollingEllipsis />
          </Typography>
        </div>
      )}

      <Snackbar
        open={errorOpen}
        autoHideDuration={2000}
        onClose={() => setErrorOpen(false)}
        anchorOrigin={{ vertical: 'top', horizontal: 'center' }}
      >
        <Alert
          onClose={() => setErrorOpen(false)}
          severity='error'
          variant='filled'
        >
          {snackbarMessage}
        </Alert>
      </Snackbar>
    </>
  );
}

export default Join;