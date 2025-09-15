import { CircularProgress } from '@mui/material';
import { AnimatePresence, motion } from 'framer-motion';
import { useEffect, useState } from 'react';

type TimerProps = {
  durationSeconds: number;
  onTimeout: () => void;
};
function Timer({ durationSeconds, onTimeout }: TimerProps) {
  const [timeLeft, setTimeLeft] = useState(durationSeconds);

  useEffect(() => {
    const start = Date.now();

    const interval = setInterval(() => {
      const elapsedSeconds = (Date.now() - start) / 1000;
      const remainingSeconds = Math.max(durationSeconds - elapsedSeconds, 0);
      setTimeLeft(remainingSeconds);

      if (remainingSeconds <= 0) {
        clearInterval(interval);
        onTimeout();
      }
    }, 50);

  return () => clearInterval(interval);
}, [durationSeconds]);

  const progress = (timeLeft / durationSeconds) * 100;
  const hue = 210 * (timeLeft / durationSeconds);
  const color = `hsl(${hue}, 100%, 50%)`;

  return (
    <div className='relative w-16 h-16'>
      {/* Track circle */}
      <CircularProgress
        variant='determinate'
        value={100}
        size={64}
        thickness={5}
        style={{ color: '#e0e0e0', position: 'absolute', left: 0, top: 0 }}
      />

      {/* Countdown circle */}
      <CircularProgress
        variant='determinate'
        value={progress}
        size={64}
        thickness={5}
        style={{ color }}
      />

      <div className='absolute inset-0 flex items-center justify-center font-bold'>
        <AnimatePresence mode='wait'>
          <motion.span
            key={Math.round(timeLeft)}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.125 }}
          >
            {Math.round(timeLeft)}
          </motion.span>
        </AnimatePresence>
      </div>
    </div>
  );
}

export default Timer;