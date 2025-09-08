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
    <span style={{ display: 'inline-block', width: '4ch', textAlign: 'left' }}>
      <AnimatePresence initial={false}>
        {Array.from({ length: 3 }, (_, i) => {
          const isVisible = i < count;
          return (
            <motion.span
              key={i}
              initial={{ opacity: 0 }}
              animate={{ opacity: isVisible ? 1 : 0 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.25 }}
              style={{ display: 'inline-block', marginRight: i < 2 ? '0.3rem' : 0 }}
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
