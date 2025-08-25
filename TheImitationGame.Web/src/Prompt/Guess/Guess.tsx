import { Button, Typography } from "@mui/material";
import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";

function Guess({connectionReady}: {connectionReady: boolean}) {
  const navigate = useNavigate();
  const location = useLocation();

  const images = location.state?.images as string[];
  const [selectedImageIndex, setSelectedImageIndex] = useState<number | null>(null);

  useEffect(() => {
    if(!connectionReady || !images) navigate('/');
  }, []);

  return (
    <>
        <Typography variant='subtitle1' sx={{ textAlign: 'center' }}>
          One of these is the real drawing, and the rest are AI imitations.
          Select the one you think is real and submit your guess.
        </Typography>

        <div className='flex flex-wrap justify-center gap-4'>
          {images.map((image, i) => {
            const isSelected = selectedImageIndex === i;
            return (
              <img
                key={i}
                onClick={() => setSelectedImageIndex(i)}
                src={`data:image/jpeg;base64,${image}`}
                className={
                  `w-[512px] h-[512px] rounded-2xl transition duration-300
                  ${isSelected ? "ring-4 ring-blue-500 shadow-xl" : "hover:shadow-2xl cursor-pointer"}`
                }
              />
            );
          })}
        </div>
        
        <Button
          variant='contained'
          color='primary'
          disabled={selectedImageIndex === null}
          onClick={() => {

          }}
        >
          Submit Guess
        </Button>
      </>
  )
}

export default Guess;