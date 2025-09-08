import CheckIcon from '@mui/icons-material/Check';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import { IconButton, Tooltip } from "@mui/material";
import { AnimatePresence, motion } from "framer-motion";
import { useState } from "react";

type CopyCodeButtonProps = {
  code: string;
};
function CopyCodeButton({ code }: CopyCodeButtonProps) {
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    await navigator.clipboard.writeText(code);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <Tooltip
      title="Copied!"
      open={copied}
      disableFocusListener
      disableHoverListener
      disableTouchListener
    >
      <IconButton edge="end" onClick={handleCopy} aria-label="Copy code">
        <AnimatePresence mode="wait" initial={false}>
          {copied ? (
            <motion.div
              key="check"
              initial={{ opacity: 0, scale: 0.5, rotate: -90 }}
              animate={{ opacity: 1, scale: 1, rotate: 0 }}
              exit={{ opacity: 0, scale: 0.5, rotate: 90 }}
              transition={{ duration: 0.25 }}
            >
              <CheckIcon className="text-green-600" />
            </motion.div>
          ) : (
            <motion.div
              key="copy"
              initial={{ opacity: 0, scale: 0.5, rotate: 90 }}
              animate={{ opacity: 1, scale: 1, rotate: 0 }}
              exit={{ opacity: 0, scale: 0.5, rotate: -90 }}
              transition={{ duration: 0.25 }}
            >
              <ContentCopyIcon className="text-gray-600 hover:text-gray-800" />
            </motion.div>
          )}
        </AnimatePresence>
      </IconButton>
    </Tooltip>
  );
}

export default CopyCodeButton;