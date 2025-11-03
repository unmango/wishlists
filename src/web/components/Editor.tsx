import type { JSX } from 'react';
import type { User } from '../api';

export interface Props {
  me: User;
}

function Editor({ me }: Props): JSX.Element {
  return (
    <div className='w-full h-full flex flex-col items-center gap-4'>
      <div className='w-1/2 text-center rounded-full bg-black/25 p-2'>
        <span className='text-white'>Hello, {me.userName}!</span>
      </div>
      <div className='w-full h-full flex gap-4'>
        <div className='h-full w-1/6 bg-black/25 rounded-xl p-2'>
          <p className='text-white'>Sidebar</p>
        </div>
        <div className='h-full w-5/6 bg-black/25 rounded-xl p-2'>
          <p className='text-white'>Main Content</p>
        </div>
      </div>
    </div>
  );
}

export default Editor;
