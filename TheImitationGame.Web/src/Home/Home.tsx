import { Button } from "@mui/material";
import { useNavigate } from 'react-router-dom';
import styles from './Home.module.css';

function Home() {
  const navigate = useNavigate();

  return (
    <div className={styles['buttons']}>
      <Button
        variant='contained'
        color='primary'
        onClick={() => navigate('/host')}
      >
        Create A Game
      </Button>

      <Button
        variant='contained'
        color='success'
        onClick={() => navigate('/join')}
      >
        Join A Game
        </Button>
    </div>
  );
}

export default Home;