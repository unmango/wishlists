import { type ButtonHTMLAttributes, type JSX } from 'react';

export interface Props {
  onCreate(): void;
  onPlan(): void;
}

export function Component({ onCreate, onPlan }: Props): JSX.Element {
  return (
    <div className='h-full flex items-center justify-center'>
      <div className='w-2/3 xl:w-1/2 flex flex-col gap-4 p-4 rounded-xl shadow-lg bg-gray-400/20 bg-clip-padding backdrop-filter backdrop-blur backdrop-saturate-100 backdrop-contrast-100'>
        <h1 className='text-8xl text-white whitespace-pre-wrap'>Get ready to get coordinated</h1>
        <div className='flex items-center justify-between text-white'>
          <Button onClick={onCreate}>Create</Button>
          <Button onClick={onPlan}>Plan</Button>
        </div>
      </div>
    </div>
  );
}

type ButtonProps = Pick<ButtonHTMLAttributes<HTMLButtonElement>, 'children' | 'onClick'>;

function Button({ children, onClick }: ButtonProps): JSX.Element {
  return (
    <button
      className='w-xs aspect-square rounded-xl bg-black/25 hover:bg-white/15 active:bg-black/50 not-active:shadow-lg cursor-pointer text-5xl'
      onClick={onClick}
    >
      {children}
    </button>
  );
}
