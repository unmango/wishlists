import { faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { JSX, PropsWithChildren } from 'react';

export interface Props extends PropsWithChildren {
  onBack: () => void;
}

export function Component({ onBack, children }: Props): JSX.Element {
  return (
    <div className='w-full h-full flex'>
      <button className='w-10 h-10 rounded-full bg-black/25 cursor-pointer' onClick={onBack}>
        <FontAwesomeIcon icon={faChevronLeft} color='white' />
      </button>
      <div className='w-full bg-black/25 bg-clip-padding backdrop-filter backdrop-blur backdrop-saturate-100 backdrop-contrast-100 rounded-xl p-4 ml-4'>
        {children}
      </div>
    </div>
  );
}
