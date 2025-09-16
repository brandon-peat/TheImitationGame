import { Button } from "@mui/material";
import { useNavigate } from 'react-router-dom';

function Home() {
  const navigate = useNavigate();

  return (
    <div className='flex justify-center items-center gap-[3rem]'>
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