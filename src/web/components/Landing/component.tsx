import { type ButtonHTMLAttributes, type JSX, useState } from 'react';
import Create from './Create';
import Plan from './Plan';

export function Component(): JSX.Element {
  // Poor man's router
  const [page, setPage] = useState<'select' | 'create' | 'plan'>('select');

  if (page === 'create') return <Create onBack={() => setPage('select')} />;
  if (page === 'plan') return <Plan onBack={() => setPage('select')} />;

  return (
    <div className='w-full h-full flex items-center justify-evenly text-white'>
      <Button onClick={() => setPage('create')}>Create</Button>
      <Button onClick={() => setPage('plan')}>Plan</Button>
    </div>
  );
}

type ButtonProps = Pick<ButtonHTMLAttributes<HTMLButtonElement>, 'children' | 'onClick'>;

function Button({ children, onClick }: ButtonProps): JSX.Element {
  return (
    <button
      className='w-sm aspect-square rounded-xl bg-black/25 hover:bg-white/15 active:bg-black/50 not-active:shadow-lg cursor-pointer text-5xl'
      onClick={onClick}
    >
      {children}
    </button>
  );
}
