import { Button, Typography } from "@mui/material";
import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";

function End() {
  const navigate = useNavigate();
  const location = useLocation();

  const won = location.state?.won;
  const wrongImage = location.state?.wrongImage;
  const realImage = location.state?.realImage;

  useEffect(() => {
    console.log(won, wrongImage, realImage);
    if(!wrongImage || !realImage || won == null) navigate('/');
  }, []);

  return (
    <>
      <Typography variant='h5'>
        {won ? 'You win!' : 'You lose!'}
      </Typography>

      <div className='flex gap-8 flex-wrap'>
        <img
          src={`data:image/jpeg;base64,${wrongImage}`}
          className='w-[512px] h-[512px] rounded-2xl ring-4 ring-red-500 shadow-xl'
        />

        <img
          src={`data:image/jpeg;base64,${realImage}`}
          className='w-[512px] h-[512px] rounded-2xl ring-4 ring-green-500 shadow-xl'
        />
      </div>

      <Typography variant='subtitle1'>
        {won ? 'Their ' : 'Your '}
        guess (red) was incorrect.
        {won ? ' Your ' : " Your opponent's "}
        drawing (green) is the real one.
      </Typography>

      <Button color='primary' variant='contained' onClick={() => navigate('/')}>
        End Game
      </Button>
    </>
  );
}

export default End;