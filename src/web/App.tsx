import { useCallback, useState } from 'react';
import { Create, Landing, Plan } from './components';

function App() {
	// Poor man's router
	const [page, setPage] = useState<'landing' | 'create' | 'plan'>('landing');

	const create = useCallback(() => setPage('create'), []);
	const plan = useCallback(() => setPage('plan'), []);
	const back = useCallback(() => setPage('landing'), []);

  return (
    <div className='h-svh w-svw flex flex-col gap-4 p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <div className='w-full flex justify-center'>
        <div className='w-2/3 text-center rounded-full bg-black/25 p-2'>
          <h1 className='text-xl text-white'>The Wishlists App</h1>
        </div>
      </div>

      <div className='w-full h-full flex flex-col items-center justify-center'>
        {page === 'landing' && <Landing onCreate={create} onPlan={plan} />}
				{page === 'create' && <Create onBack={back} />}
				{page === 'plan' && <Plan onBack={back} />}
      </div>
    </div>
  );
}

export default App;
