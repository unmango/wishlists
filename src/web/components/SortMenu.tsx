import { faCheck, faChevronDown } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useId, useState } from 'react';
import { type Sort, sortLabel, sorts } from '../sorting/sorts';

type Props = {
  sort: Sort;
  onChange: (sort: Sort) => void;
  /** The owner's first name, or undefined on your own wishlist. */
  ownerName: string | undefined;
};

/** A small "Sort: Bob's order ▾" button that opens the four choices. */
export function SortMenu({ sort, onChange, ownerName }: Props) {
  const [open, setOpen] = useState(false);
  const menuId = useId();

  function choose(next: Sort) {
    onChange(next);
    setOpen(false);
  }

  return (
    <div className='relative'>
      <button
        type='button'
        aria-haspopup='menu'
        aria-expanded={open}
        aria-controls={menuId}
        onClick={() => setOpen(x => !x)}
        className='flex h-11 items-center gap-1.5 rounded-full px-2 text-sm text-soft'
      >
        <span>
          Sort: <span className='font-medium text-ink'>{sortLabel(sort, ownerName)}</span>
        </span>
        <FontAwesomeIcon icon={faChevronDown} className='text-xs' aria-hidden='true' />
      </button>

      {open && (
        <>
          {/* Tapping anywhere else closes the menu without changing the sort. */}
          <button
            type='button'
            aria-label='Close sort menu'
            tabIndex={-1}
            onClick={() => setOpen(false)}
            className='fixed inset-0 z-10 cursor-default'
          />
          <ul
            id={menuId}
            role='menu'
            onKeyDown={e => e.key === 'Escape' && setOpen(false)}
            className='absolute top-full left-0 z-20 m-0 flex min-w-56 list-none flex-col rounded-[10px] bg-card p-1.5 shadow-[0_6px_16px_rgba(0,0,0,0.2)]'
          >
            {sorts.map(x => (
              <li key={x} role='none'>
                <button
                  type='button'
                  role='menuitemradio'
                  aria-checked={x === sort}
                  autoFocus={x === sort}
                  onClick={() => choose(x)}
                  className='flex h-11 w-full items-center gap-2 rounded-md px-3 text-left text-[15px] text-ink hover:bg-chip focus-visible:bg-chip'
                >
                  <span className='w-4 text-primary'>
                    {x === sort && <FontAwesomeIcon icon={faCheck} aria-hidden='true' />}
                  </span>
                  {sortLabel(x, ownerName)}
                </button>
              </li>
            ))}
          </ul>
        </>
      )}
    </div>
  );
}
