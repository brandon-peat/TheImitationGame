import Typography from '@mui/material/Typography';
import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import connection from '../signalr-connection';

function Draw({connectionReady}: {connectionReady: boolean}) {
  const navigate = useNavigate();

  useEffect(() => {
    if(!connectionReady) navigate('/');

    return () => {
      connection.invoke('LeaveGame')
        .catch((error) => {
          console.error('Error leaving game:', error);
        });
    }
  });

  return (
    <Typography variant='subtitle1' sx={{ textAlign: 'center' }}>
      Your opponent is coming up with a prompt. Get ready to draw!
    </Typography>
  );
}

export default Draw;