import { Button, Typography } from "@mui/material";
import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";

function Lose() {
  const navigate = useNavigate();
  const location = useLocation();
  const selectedImage = location.state?.selectedImage;
  const realImage = location.state?.realImage;

  useEffect(() => {
    if(!selectedImage || !realImage) navigate('/');
  }, []);

  return (
    <>
      <Typography variant='h5'>
        You lose!
      </Typography>

      <div className='flex gap-8 flex-wrap'>
        <img
          src={`data:image/jpeg;base64,${selectedImage}`}
          className='w-[512px] h-[512px] rounded-2xl ring-4 ring-red-500 shadow-xl'
        />

        <img
          src={`data:image/jpeg;base64,${realImage}`}
          className='w-[512px] h-[512px] rounded-2xl ring-4 ring-green-500 shadow-xl'
        />
      </div>

      <Typography variant='subtitle1'>
        Your guess (red) was incorrect. Your opponent's drawing (green) is the real one.
      </Typography>

      <Button color='primary' variant='contained' onClick={() => navigate('/')}>
        End Game
      </Button>
    </>
  );
}

export default Lose;