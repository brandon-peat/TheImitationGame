import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { Route, Routes, useLocation, useNavigate, useNavigationType } from 'react-router-dom';
import Draw from './Routes/Draw/Draw';
import End from './Routes/End';
import Guess from './Routes/Guess';
import Home from './Routes/Home';
import Host from './Routes/Host/Host';
import Join from './Routes/Join';
import NextRound from './Routes/NextRound';
import Prompt from './Routes/Prompt';
import connection from './Utilities/signalr-connection';

function App() {
  const [connectionReady, setConnectionReady] = useState(false);
  const [message, setMessage] = useState('');

  const navigationType = useNavigationType();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    if (connection.state === 'Disconnected') {
      connection.start().then(() => setConnectionReady(true));
    }

    const handleOtherPlayerLeft = () => {
      navigate('/');
      setMessage('The other player left the game, so you were disconnected.');
    };
    connection.on('HostLeft', handleOtherPlayerLeft);
    connection.on('JoinerLeft', handleOtherPlayerLeft);

    return () => {
      connection.off('HostLeft', handleOtherPlayerLeft);
      connection.off('JoinerLeft', handleOtherPlayerLeft);
    }
  }, []);

  useEffect(() => {
    if (navigationType === 'POP' && connectionReady) {
      navigate('/');
      connection.invoke('LeaveGame').catch((error) => {
        console.error('Error leaving game:', error);
      });
    }
  }, [navigationType]);

  useEffect(() => {
    if(location.pathname !== '/') {
      setMessage('');
      if(!connectionReady) navigate('/');
    }
  }, [location.pathname]);

  return (
    <div className='flex flex-col items-center gap-[1.5rem] p-[1.5rem] w-[60vw] min-w-md m-auto text-center'>
      <Typography variant='h3'>
        The Imitation Game
      </Typography>

      <Typography gutterBottom variant='body2'>
        In this game, you and your opponent will alternate between giving prompts, drawing from those
        prompts, and trying to tell apart the real drawing from AI fakes.
        <br />
        As the prompter, your goal is to tell the difference between the AI fakes and your opponent's drawing.
        <br />
        As the drawer, your goal is to create a drawing from the prompt that will be hard to distinguish from the AI fakes.
      </Typography>

      <Routes>
        <Route path='/' element={<Home />} />
        <Route path='/host' element={<Host />} />
        <Route path='/join' element={<Join />} />
        <Route path='/prompt' element={<Prompt />} />
        <Route path='/guess' element={<Guess />} />
        <Route path='/draw' element={<Draw />} />
        <Route path='/next-round' element={<NextRound />} />
        <Route path='/end' element={<End />} />
      </Routes>

      {message && (
        <Typography variant='body2'>
          {message}
        </Typography>
      )}
    </div>
  );
}

export default App;