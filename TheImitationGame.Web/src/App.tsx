import Typography from '@mui/material/Typography';
import { useEffect } from 'react';
import { Route, BrowserRouter as Router, Routes } from 'react-router-dom';
import './App.css';
import Home from './Home/Home';
import Host from './Host/Host';
import Join from './Join/Join';
import connection from './signalr-connection';


function App() {
  useEffect(() => {
    // LATER: turn on listeners here
    
    if (connection.state === 'Disconnected') {
      connection.start();
    }

    return () => {
      // LATER: turn off listeners here
    }
  }, []);

  return (
    <div className='page'>
      <Typography variant='h3' gutterBottom sx={{ textAlign: 'center' }}>
        The Imitation Game
      </Typography>

      <Router>
        <Routes>
          <Route path = '/' element={<Home />} />
          <Route path = '/host' element={<Host />} />
          <Route path = '/join' element={<Join />} />
        </Routes>
      </Router>
    </div>
  );
}

export default App;