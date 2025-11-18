import { useCallback } from 'react';
import { useQuery } from './api';
import { NoWishlists, Workspace } from './components';

function App() {
  const { data, error, isLoading } = useQuery('/api/wishlists');

  const back = useCallback(() => {}, []);

  return (
    <div className='h-svh w-svw p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <Workspace onBack={back}>
				{isLoading && <p className='text-white'>Loading...</p>}
				{error && <p className='text-red-500'>Error: {String(error)}</p>}
				{!data?.length && <NoWishlists onCreate={() => {}} />}
      </Workspace>
    </div>
  );
}

export default App;
