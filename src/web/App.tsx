import { useCallback } from 'react';
import { NoWishlists, Workspace } from './components';
import { useWishlists } from './hooks';

function App() {
  const { data, error, isLoading } = useWishlists();

  const back = useCallback(() => {}, []);
  const signIn = useCallback(() => window.location.assign('/auth/login'), []);

  return (
    <div className='h-svh w-svw p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <Workspace onBack={back}>
        {isLoading && <p className='text-white'>Loading...</p>}
        {error && <p className='text-red-500'>Error: {String(error)}</p>}
        {!isLoading && !data && <NoWishlists onCreate={signIn} />}
        {data && <p className='text-white'>{data.name}: {data.items.length} items</p>}
      </Workspace>
    </div>
  );
}

export default App;
