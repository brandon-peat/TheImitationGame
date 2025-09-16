import { Button, Typography } from "@mui/material";
import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import connection from "../Utilities/signalr-connection";

type Role = 'prompter' | 'drawer';

function NextRound() {
  const navigate = useNavigate();
  const location = useLocation();

  const roundNumber: number = location.state?.roundNumber;
  const currentRole: Role = location.state?.role;
  const isHost: boolean = location.state?.isHost;

  useEffect(() => {
    if(!roundNumber || !currentRole || isHost === undefined) navigate('/');

    const handleRoundStartedAsDrawer = () => {
      navigate('/draw');
    }
    const handleRoundStartedAsPrompter = (defaultPrompt: string) => {
      navigate('/prompt', { state: { defaultPrompt } });
    }

    if(currentRole === 'prompter')
      connection.on('AwaitPrompt', handleRoundStartedAsDrawer);

    else if(currentRole === 'drawer')
      connection.on('PromptTimerStarted', handleRoundStartedAsPrompter);

    return () => {
      if(currentRole === 'prompter') 
        connection.off('AwaitPrompt', handleRoundStartedAsDrawer);

      else if(currentRole === 'drawer')
        connection.off('PromptTimerStarted', handleRoundStartedAsPrompter);
    }
  }, []);

  return (
    <>
      <Typography variant='h5'>
        Starting Round {roundNumber}!
      </Typography>

      <Typography variant='subtitle1'>
        Since you were the {currentRole} last round,
        you will be the {currentRole === 'prompter' ? 'drawer' : 'prompter'} this round.
      </Typography>

      {isHost ? (
        <Button
          variant='contained'
          color='primary'
          onClick={() => {
            connection.invoke('StartNextRound')
              .catch((error) => {
                console.error('Error starting next round:', error);
              });
          }}
        >
          Start Round {roundNumber}
        </Button>
      ) : (
        <Typography variant='subtitle2'>
          Waiting for the host to start the next round...
        </Typography>
      )}
    </>
  );
}

export default NextRound;