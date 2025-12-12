import type { JSX, PropsWithChildren } from 'react';

export interface Props extends PropsWithChildren {
  className?: string;
}

export function Pane({ children, className }: Props): JSX.Element {
  return (
    <div
      className={[
        'rounded-2xl bg-gray-800/10 drop-shadow-lg bg-clip-padding backdrop-filter backdrop-blur backdrop-saturate-100 backdrop-contrast-100',
        className,
      ].join(' ')}
    >
      {children}
    </div>
  );
}
