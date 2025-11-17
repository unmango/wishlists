import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { JSX } from 'react';

export interface Props {
  onBack: () => void;
}

export function Component({ onBack }: Props): JSX.Element {
  return (
    <div className='w-full h-full flex'>
      <button className='w-10 h-10 rounded-full bg-black/25' onClick={onBack}>
        <FontAwesomeIcon icon={faChevronLeft} color='white' />
      </button>
    </div>
  );
}
