import HourglassBottomIcon from "@mui/icons-material/HourglassBottom";
import HourglassTopIcon from "@mui/icons-material/HourglassTop";
import { motion, useAnimation } from "framer-motion";
import { useEffect, useState } from "react";

function WaitingSpinner() {
  const controls = useAnimation();
  const [flipped, setFlipped] = useState(false);

  useEffect(() => {
    let rotation = 0;
    const interval = setInterval(() => {
      rotation += 180;
      controls.start({ rotate: rotation });
      setFlipped((f) => !f);
    }, 1300);

    return () => clearInterval(interval);
  }, [controls]);

  return (
    <motion.div
      className='relative w-6 h-6 text-gray-500 origin-[50%_50%]'
      animate={controls}
      transition={{ duration: 0.8, ease: 'easeInOut' }}
    >
      <motion.div
        className='absolute inset-0 flex items-center justify-center'
        initial={{ opacity: 0 }}
        animate={{ opacity: flipped ? 1 : 0 }}
        transition={{ duration: 0.3, ease: 'easeInOut', delay: 0.6 }}
      >
        <HourglassTopIcon />
      </motion.div>

      <motion.div
        className='absolute inset-0 flex items-center justify-center'
        initial={{ opacity: 1 }}
        animate={{ opacity: flipped ? 0 : 1 }}
        transition={{ duration: 0.3, ease: 'easeInOut', delay: 0.6 }}
      >
        <HourglassBottomIcon />
      </motion.div>
    </motion.div>
  );
}

export default WaitingSpinner;