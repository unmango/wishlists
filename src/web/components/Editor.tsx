import { type JSX } from 'react';

function Editor(): JSX.Element {
  return (
    <div className='w-full h-full flex flex-col items-center gap-4'>
      <div className='w-full text-center rounded-full bg-black/25 p-2'>
        <span className='text-white'>Hello, TODO!</span>
      </div>
      <div className='w-full h-full flex gap-4'>
        <div className='h-full w-1/6 bg-black/25 rounded-xl p-2'>
        </div>
        <div className='h-full w-5/6 bg-black/25 rounded-xl p-2'>
        </div>
      </div>
    </div>
  );
}

export default Editor;
