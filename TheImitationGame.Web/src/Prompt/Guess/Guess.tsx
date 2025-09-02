import { Button, Typography } from "@mui/material";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import connection from "../../signalr-connection";

function Guess() {
  const navigate = useNavigate();

  const [images, setImages] = useState<string[]>([]);
  const [selectedImageIndex, setSelectedImageIndex] = useState<number | null>(null);
  const [guessSubmitted, setGuessSubmitted] = useState(false);

  useEffect(() => {
    const handleGuessTimerStarted = (images: string[]) => {
      setImages(images);
    }
    connection.on('GuessTimerStarted', handleGuessTimerStarted);

    const handleCorrectGuess = (roundNumber: number) => {
      navigate('/next-round', { state: { roundNumber: roundNumber, role: 'prompter', isHost: true } });
    }
    connection.on('CorrectGuess-StartBetweenRoundsPhase', handleCorrectGuess);

    const handleCorrectGuessAsJoiner = (roundNumber: number) => {
      navigate('/next-round', { state: { roundNumber: roundNumber, role: 'prompter', isHost: false } });
    }
    connection.on('CorrectGuess-AwaitNextRoundStart', handleCorrectGuessAsJoiner);
    
    return () => {
      connection.off('GuessTimerStarted', handleGuessTimerStarted);
      connection.off('CorrectGuess-StartBetweenRoundsPhase', handleCorrectGuess);
      connection.on('CorrectGuess-AwaitNextRoundStart', handleCorrectGuessAsJoiner);
    }
  }, []);

  useEffect(() => {
    const handleLose = (realImageIndex: number) => {
      var wrongImage = images[selectedImageIndex!];
      var realImage = images[realImageIndex];
      navigate('/end', {state: {won: false, wrongImage, realImage}});
    }
    connection.on('IncorrectGuess-Lose', handleLose);

    return () => {
      connection.off('IncorrectGuess-Lose', handleLose);
    }
  }, [images, selectedImageIndex]);

  return (
    (images.length === 0) ? (
      <Typography variant='subtitle1' sx={{ textAlign: 'center' }}>
        AI is generating imitations . . .
      </Typography>
    ) : (
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
                  ${isSelected ? 'ring-4 ring-blue-500 shadow-xl' : 'hover:shadow-2xl cursor-pointer'}`
                }
              />
            );
          })}
        </div>
        
        <Button
          variant='contained'
          color='primary'
          disabled={selectedImageIndex === null || guessSubmitted}
          onClick={() => {
            setGuessSubmitted(true);
            connection.invoke('SubmitGuess', selectedImageIndex)
              .catch((error) => {
                console.error('Error submitting guess:', error);
              });
          }}
        >
          Submit Guess
        </Button>
      </>
    )
  )
}

export default Guess;