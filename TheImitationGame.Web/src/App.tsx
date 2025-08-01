import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { Route, Routes, useLocation, useNavigate } from 'react-router-dom';
import './App.css';
import Draw from './Draw/Draw';
import Home from './Home/Home';
import Host from './Host/Host';
import Join from './Join/Join';
import Prompt from './Prompt/Prompt';
import connection from './signalr-connection';


function App() {
  const [connectionReady, setConnectionReady] = useState(false);
  const [message, setMessage] = useState('');

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
    if(location.pathname !== '/') setMessage('');
  }, [location.pathname]);

  return (
    <div className='page'>
      <Typography variant='h3' sx={{ textAlign: 'center' }}>
        The Imitation Game
      </Typography>

      <Typography gutterBottom variant='body2' sx={{ textAlign: 'center' }}>
        In this game, you and your opponent will alternate between giving prompts, drawing from those
        prompts, and trying to tell apart the real drawing from AI fakes.
        <br />
        As the prompter, your goal is to tell the difference between the AI fakes and your opponent's drawing.
        <br />
        As the drawer, your goal is to create a drawing from the prompt that will be hard to distinguish from the AI fakes.
      </Typography>

      <Routes>
        <Route path = '/' element={<Home />} />
        <Route path = '/host' element={<Host connectionReady={connectionReady} />} />
        <Route path = '/join' element={<Join connectionReady={connectionReady} />} />
        <Route path = '/prompt' element={<Prompt connectionReady={connectionReady} />} />
        <Route path = '/draw' element={<Draw connectionReady={connectionReady} />} />
      </Routes>

      {message && (
        <Typography variant='body2' sx={{ textAlign: 'center' }}>
          {message}
        </Typography>
      )}
    </div>
  );
}

export default App;