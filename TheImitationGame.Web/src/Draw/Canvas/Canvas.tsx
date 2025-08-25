import AutoFixNormalIcon from '@mui/icons-material/AutoFixNormal';
import CreateIcon from '@mui/icons-material/Create';
import RedoIcon from '@mui/icons-material/Redo';
import ReplayIcon from '@mui/icons-material/Replay';
import UndoIcon from '@mui/icons-material/Undo';
import { Button, IconButton } from '@mui/material';
import { useRef, useState } from 'react';
import { ReactSketchCanvas, type ReactSketchCanvasRef } from 'react-sketch-canvas';

type CanvasProps = {
  onSubmitDrawing: (imageDataUrl: string) => void;
  submitDisabled: boolean;
};
function Canvas({ onSubmitDrawing, submitDisabled }: CanvasProps) {
  const canvasRef = useRef<ReactSketchCanvasRef>(null);
  const [eraseMode, setEraseMode] = useState(false);

  async function handleSubmitClick() {
    if (canvasRef.current) {
      const imageDataUrl = await canvasRef.current.exportImage('jpeg');
      onSubmitDrawing(imageDataUrl);
    }
  }
  return (
    <div className='flex gap-4 justify-center w-4/5 h-4/5'>
      <div className='flex flex-col  gap-y-2'>
        <ReactSketchCanvas
          ref={canvasRef}
          strokeWidth={4}
          strokeColor='black'
          className='!w-[512px] !h-[512px]'
        />

        <Button
          className='w-full'
          variant='contained'
          color='primary'
          onClick={handleSubmitClick}
          disabled={submitDisabled}
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