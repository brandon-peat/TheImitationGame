import AutoFixNormalIcon from '@mui/icons-material/AutoFixNormal';
import CreateIcon from '@mui/icons-material/Create';
import RedoIcon from '@mui/icons-material/Redo';
import ReplayIcon from '@mui/icons-material/Replay';
import UndoIcon from '@mui/icons-material/Undo';
import { Button, IconButton } from '@mui/material';
import { useRef, useState } from 'react';
import { ReactSketchCanvas, type ReactSketchCanvasRef } from 'react-sketch-canvas';

function Canvas() {
  const canvasRef = useRef<ReactSketchCanvasRef>(null);
  const [eraseMode, setEraseMode] = useState(false);

  function handleSubmitClick() {
    // TODO: callback to Draw to submit
  }
  return (
    <div className='flex gap-4 w-4/5 h-4/5'>
      <div className='flex flex-col gap-y-2 w-full h-9/10'>
        <ReactSketchCanvas
          ref={canvasRef}
          strokeWidth={4}
          strokeColor='black'
        />

        <Button
          className='w-full'
          variant='contained'
          color='primary'
          onClick={handleSubmitClick}
        >
          Submit Drawing
        </Button>
      </div>

      <div className='flex flex-col gap-y-10'>
        <div className='flex flex-col gap-y-2'>
          <IconButton
            onClick={() => {
              setEraseMode(false);
              canvasRef.current?.eraseMode(false);
            }}
            disabled={!eraseMode}
          >
            <CreateIcon />
          </IconButton>

          <IconButton
            onClick={() => {
              setEraseMode(true);
              canvasRef.current?.eraseMode(true);
            }}
            disabled={eraseMode}
          >
            <AutoFixNormalIcon />
          </IconButton>
        </div>

        <div className='flex flex-col gap-y-2'>
          <IconButton
            onClick={() => canvasRef.current?.undo()}
          >
            <UndoIcon />
          </IconButton>

          <IconButton
            onClick={() => canvasRef.current?.redo()}
          >
            <RedoIcon />
          </IconButton>

          <IconButton
            onClick={() => canvasRef.current?.clearCanvas()}
          >
            <ReplayIcon />
          </IconButton>
        </div>
      </div>
    </div>
  );
}

export default Canvas;