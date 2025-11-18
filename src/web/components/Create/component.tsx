import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { JSX } from 'react';
import { useQuery } from '../../api';

export interface Props {
  onBack: () => void;
}

export function Component({ onBack }: Props): JSX.Element {
  const { isLoading, data, error } = useQuery('/api/wishlists');

  if (isLoading) return <p className='text-white'>Loading...</p>;
  if (error) return <p className='text-red-500'>Error: {String(error)}</p>;

	// if (data?.length) console.log('heheheheh');
	if (!data?.length) console.log('heheheheh');

  return (
    <div className='w-full h-full flex'>
      <button className='w-10 h-10 rounded-full bg-black/25' onClick={onBack}>
        <FontAwesomeIcon icon={faChevronLeft} color='white' />
      </button>
      {!data?.length && <pre>{JSON.stringify(data)}</pre>}
      <div className='w-12 bg-red-500'>A</div>
    </div>
  );
}
