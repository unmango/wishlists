import { type ButtonHTMLAttributes, type JSX } from 'react';

export interface Props {
	onCreate(): void;
	onPlan(): void;
}

export function Component({ onCreate, onPlan }: Props): JSX.Element {
  return (
    <div className='w-full h-full flex items-center justify-evenly text-white'>
      <Button onClick={onCreate}>Create</Button>
      <Button onClick={onPlan}>Plan</Button>
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
