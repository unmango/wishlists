import { useEffect, useState } from 'react';
import { client, type User } from './api';
import Editor from './components/Editor';
import Register from './components/Register';

function App() {
  const [me] = useState<User | null>(null);

  useEffect(() => {
    // api.me().then(setMe).catch(console.error);
  }, []);

  return (
    <div className='h-svh w-svw flex flex-col gap-4 p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <div className='w-full flex justify-center'>
        <div className='w-1/3 text-center rounded-full bg-black/25 p-2'>
          <h1 className='text-xl text-white'>The Wishlists App</h1>
        </div>
      </div>
      {me ? <Editor me={me} /> : <Register client={client} />}
    </div>
  );
}

export default App;
