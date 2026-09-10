import type { InputHTMLAttributes, JSX } from 'react';

type InputProps = InputHTMLAttributes<HTMLInputElement>;

export interface Props extends Omit<InputProps, 'onChange'> {
  onChange(value: string): void;
}

function TextBox({ onChange, ...props }: Props): JSX.Element {
  return (
    <input
      {...props}
      className='w-full px-4 py-2 rounded-full bg-black/15 text-white placeholder-white/50 focus:outline-none focus:ring-2 focus:ring-white/50 focus:bg-white/15'
      onChange={(e) => onChange(e.target.value)}
    />
  );
}

export default TextBox;
