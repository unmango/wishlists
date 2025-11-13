import type { JSX } from 'react';

export interface Props {
  onBack: () => void;
}

export function Component({ onBack }: Props): JSX.Element {
  return (
    <div className='w-full h-full flex items-center justify-center text-white'>
      <button className='w-sm aspect-square bg-black/25' onClick={onBack}>
        Back
      </button>
    </div>
  );
}
