import Typography from '@mui/material/Typography';
import { useEffect, useState } from 'react';
import { Route, BrowserRouter as Router, Routes } from 'react-router-dom';
import './App.css';
import Home from './Home/Home';
import Host from './Host/Host';
import Join from './Join/Join';
import Prompt from './Prompt/Prompt';
import connection from './signalr-connection';


function App() {
  const [connectionReady, setConnectionReady] = useState(false);

  useEffect(() => {
    // LATER: turn on listeners here
    
    if (connection.state === 'Disconnected') {
      connection.start().then(() => setConnectionReady(true));
    }

    return () => {
      // LATER: turn off listeners here
    }
  }, []);

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

      <Router>
        <Routes>
          <Route path = '/' element={<Home />} />
          <Route path = '/host' element={<Host connectionReady={connectionReady} />} />
          <Route path = '/join' element={<Join connectionReady={connectionReady} />} />
          <Route path = '/prompt' element={<Prompt connectionReady={connectionReady} />} />
        </Routes>
      </Router>
    </div>
  );
}

export default App;