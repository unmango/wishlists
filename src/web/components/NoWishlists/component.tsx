import type { JSX } from 'react';

export interface Props {
  onCreate(): void;
}

export function Component({ onCreate }: Props): JSX.Element {
  return (
    <div className='w-full h-full flex flex-col gap-4 items-center justify-center text-white'>
      <h2 className='text-4xl'>No wishlists found</h2>
      <p className='text-lg'>Click "Create" to make your first wishlist!</p>
      <button className='px-8 py-2 rounded-full bg-black/25 cursor-pointer' onClick={onCreate}>
        Create
      </button>
    </div>
  );
}
