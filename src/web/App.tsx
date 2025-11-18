import { useCallback } from 'react';
import { NoWishlists, Workspace } from './components';
import { useWishlists } from './hooks';
import { v7 as uuid } from 'uuid'

function App() {
  const { data, error, isLoading, create } = useWishlists();

  const back = useCallback(() => {}, []);

  return (
    <div className='h-svh w-svw p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <Workspace onBack={back}>
        {isLoading && <p className='text-white'>Loading...</p>}
        {error && <p className='text-red-500'>Error: {String(error)}</p>}
        {!data?.length && <NoWishlists onCreate={() => create('Test', uuid())} />}
      </Workspace>
    </div>
  );
}

export default App;
