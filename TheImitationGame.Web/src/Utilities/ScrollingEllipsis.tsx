import { AnimatePresence, motion } from 'framer-motion';
import { useEffect, useState } from 'react';

function ScrollingEllipsis() {
  const [count, setCount] = useState(1);

  useEffect(() => {
    const interval = setInterval(() => {
      setCount((c) => (c % 3) + 1);
    }, 500);
    return () => clearInterval(interval);
  }, []);

  return (
    <span className='text-left inline-block'>
      <AnimatePresence initial={false}>
        {Array.from({ length: 3 }, (_, i) => {
          const isVisible = i < count;
          return (
            <motion.span
              className={'inline-block mr-[0.3rem]'}
              key={i}
              initial={{ opacity: 0 }}
              animate={{ opacity: isVisible ? 1 : 0 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.25 }}
            >
              {'.'}
            </motion.span>
          );
        })}
      </AnimatePresence>
    </span>
  );
}

export default ScrollingEllipsis;
