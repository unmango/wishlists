import { useEffect, useState } from 'react';
import type { User } from './api';
import * as api from './api';

function App() {
  const [me, setMe] = useState<User | null>(null);

  useEffect(() => {
    api.me().then(setMe).catch(console.error);
  }, []);

  return (
    <div className='h-svh w-svw flex flex-col gap-4 p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <div className='w-full flex justify-center'>
        <div className='w-1/3 text-center rounded-full bg-black/25 p-2'>
          <h1 className='text-xl text-white'>The Wishlists App</h1>
        </div>
      </div>
      <div className='w-full h-full flex flex-col items-center gap-4'>
        <div className='w-1/2 text-center rounded-full bg-black/25 p-2'>
          <span className='text-white'>Hello, {me?.name}!</span>
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
    </div>
  );
}

export default App;
