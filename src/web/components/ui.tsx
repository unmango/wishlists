import { faBagShopping, faGift, faStar, faUsers } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import type { ReactNode } from 'react';
import { href, type Route } from '../hooks';

const tints = ['#0f6e7a', '#c2362c', '#8f5b12', '#6b3f86', '#2f5f9e', '#3d6b2f'];

function tintFor(id: string): string {
  let hash = 0;
  for (const c of id) hash = (hash * 31 + c.charCodeAt(0)) | 0;
  return tints[Math.abs(hash) % tints.length]!;
}

export function Avatar({ id, name, url }: { id: string; name: string; url?: string | null }) {
  if (url) {
    return <img src={url} alt='' referrerPolicy='no-referrer' className='size-11 shrink-0 rounded-full object-cover' />;
  }

  return (
    <div
      aria-hidden='true'
      className='flex size-11 shrink-0 items-center justify-center rounded-full text-lg font-bold text-white'
      style={{ background: tintFor(id) }}
    >
      {name.trim().charAt(0).toUpperCase() || '?'}
    </div>
  );
}

export function ReallyWantStar({ label }: { label: string }) {
  return <FontAwesomeIcon icon={faStar} className='text-sm text-accent' aria-label={label} title={label} />;
}

/** Page frame: a phone-width column with room for the tab bar. */
export function Screen({ children }: { children: ReactNode }) {
  return <div className='mx-auto min-h-svh max-w-md pb-28'>{children}</div>;
}

export function PageHeader({ eyebrow, title, action }: { eyebrow?: string; title: string; action?: ReactNode }) {
  return (
    <header className='flex items-end gap-2 px-5 pt-14 pb-4'>
      <div className='flex min-w-0 flex-1 flex-col gap-1'>
        {eyebrow && <div className='text-sm text-muted'>{eyebrow}</div>}
        <h1 className='m-0 font-display text-[32px] leading-tight font-extrabold tracking-tight break-words'>
          {title}
        </h1>
      </div>
      {action}
    </header>
  );
}

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return <div className={`rounded-[10px] bg-card shadow-[0_1px_2px_rgba(0,0,0,0.06)] ${className}`}>{children}</div>;
}

export function Loading() {
  return <p className='px-5 py-8 text-center text-muted'>Loading…</p>;
}

export function ErrorMessage({ error }: { error: unknown }) {
  const message = error instanceof Error ? error.message : 'Something went wrong.';
  return (
    <p role='alert' className='mx-4 rounded-[10px] bg-accent-soft px-4 py-3 text-sm text-danger'>
      {message}
    </p>
  );
}

const tabs: { route: Route; label: string; icon: typeof faUsers }[] = [
  { route: { name: 'family' }, label: 'Family', icon: faUsers },
  { route: { name: 'mine' }, label: 'My list', icon: faGift },
  { route: { name: 'buying' }, label: 'Buying', icon: faBagShopping },
];

export function TabBar({ active }: { active: Route['name'] }) {
  // Someone else's list belongs under Family.
  const current = active === 'list' ? 'family' : active;

  return (
    <nav className='fixed inset-x-0 bottom-0 border-t border-line bg-card pb-[max(env(safe-area-inset-bottom),12px)]'>
      <ul className='mx-auto grid max-w-md grid-cols-3'>
        {tabs.map(tab => {
          const selected = tab.route.name === current;
          return (
            <li key={tab.label}>
              <a
                href={href(tab.route)}
                aria-current={selected ? 'page' : undefined}
                className={`flex min-h-14 flex-col items-center justify-center gap-1 pt-2 text-xs ${
                  selected ? 'font-bold text-primary' : 'text-muted'
                }`}
              >
                <FontAwesomeIcon icon={tab.icon} className='text-xl' aria-hidden='true' />
                {tab.label}
              </a>
            </li>
          );
        })}
      </ul>
    </nav>
  );
}
