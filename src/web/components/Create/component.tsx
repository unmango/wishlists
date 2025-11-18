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

  return (
    <div className='w-full h-full flex'>
      <button className='w-10 h-10 rounded-full bg-black/25' onClick={onBack}>
        <FontAwesomeIcon icon={faChevronLeft} color='white' />
      </button>
      <div className='w-full bg-black/20 bg-clip-padding backdrop-filter backdrop-blur backdrop-saturate-100 backdrop-contrast-100 rounded-xl p-4 ml-4'>
        {!data?.length && <EmptyList />}
      </div>
    </div>
  );
}

function EmptyList(): JSX.Element {
  return (
    <div className='w-full h-full flex flex-col items-center justify-center text-white'>
      <h2 className='text-4xl mb-4'>No wishlists found</h2>
      <p className='text-lg'>Click "Create" to make your first wishlist!</p>
    </div>
  );
}
