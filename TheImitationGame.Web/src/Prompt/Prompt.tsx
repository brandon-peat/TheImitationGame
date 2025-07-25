import { TextField } from "@mui/material";
import { useEffect } from "react";
import { useLocation, useNavigate } from 'react-router-dom';
import connection from "../signalr-connection";

function Prompt({connectionReady}: {connectionReady: boolean}) {
  const navigate = useNavigate();
  const location = useLocation();
  const defaultPrompt = location.state?.defaultPrompt;

  useEffect(() => {
    if(!connectionReady || !defaultPrompt) navigate('/');

    return () => {
      connection.invoke('LeaveGame')
        .catch((error) => {
          console.error('Error leaving game:', error);
        });
    }
  });

  return (
    <TextField sx={{ width: '60%' }}
      label={'What should your opponent draw?'}
      defaultValue={defaultPrompt}
      variant={'outlined'}
      onKeyDown={(e) => {
        if (e.key === 'Enter') {
          let input = e.target as HTMLInputElement;
          // TODO: send it to the hub
          input.value = '';
        }
      }}
    />
  );
}

export default Prompt;