import { useQuery } from './api';
import { Landing } from './components';

function App() {
  const { isLoading, data, error } = useQuery('/api/wishlists');

  return (
    <div className='h-svh w-svw flex flex-col gap-4 p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <div className='w-full flex justify-center'>
        <div className='w-2/3 text-center rounded-full bg-black/25 p-2'>
          <h1 className='text-xl text-white'>The Wishlists App</h1>
        </div>
      </div>

      <div className='w-full h-full flex flex-col items-center justify-center'>
        {isLoading && <p className='text-white'>Loading...</p>}
        {error && <p className='text-red-500'>Error: {String(error)}</p>}
        {data && <Landing wishlists={data} /> || <p className='text-white'>No data available.</p>}
      </div>
    </div>
  );
}

export default App;
