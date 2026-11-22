import { faGripLines } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { type KeyboardEvent, type PointerEvent, useEffect, useId, useRef, useState } from 'react';
import type { OwnItem } from '../api';
import { move } from '../sorting/sorts';
import { OwnItemSummary } from './OwnItemSummary';
import { Card, ErrorMessage } from './ui';

type Props = {
  items: OwnItem[];
  saving: boolean;
  error: unknown;
  /** Saves the new order: item ids, top first. */
  onDone: (itemIds: string[]) => void;
  onCancel: () => void;
};

/** How near the top or bottom of the screen, in pixels, a drag scrolls the page. The bottom allows for the tab bar. */
const scrollEdge = { top: 80, bottom: 140 };

/** A ref callback that keeps `elements` in step with what's rendered. */
function register<T extends HTMLElement>(elements: Map<string, T>, id: string) {
  return (el: T | null) => {
    if (el) elements.set(id, el);
    else elements.delete(id);
  };
}

/** Puts the dragged item after every other item whose middle is above the pointer. */
function dragTo(ids: string[], id: string, y: number, rows: Map<string, HTMLElement>): string[] {
  const from = ids.indexOf(id);
  const to = ids.filter(x => {
    const rect = x !== id && rows.get(x)?.getBoundingClientRect();
    return rect && y > rect.top + rect.height / 2;
  }).length;
  return from === to ? ids : move(ids, from, to);
}

/** The owner's wishlist with drag handles. Nothing is saved until Done. */
export function ReorderList({ items, saving, error, onDone, onCancel }: Props) {
  const [ids, setIds] = useState(() => items.map(x => x.id));
  const [dragging, setDragging] = useState<string>();
  const rows = useRef(new Map<string, HTMLLIElement>());
  const handles = useRef(new Map<string, HTMLButtonElement>());
  const pointerY = useRef(0);
  const hintId = useId();
  const byId = new Map(items.map(x => [x.id, x]));

  // While the owner drags an item near the top or bottom of the screen, scroll the page, so they can move items end to end in long lists.
  useEffect(() => {
    if (!dragging) return;
    let frame = 0;
    const tick = () => {
      const y = pointerY.current;
      const bottom = window.innerHeight - scrollEdge.bottom;
      const speed = y < scrollEdge.top ? (y - scrollEdge.top) / 4 : y > bottom ? (y - bottom) / 4 : 0;
      if (speed) {
        window.scrollBy(0, speed);
        setIds(current => dragTo(current, dragging, y, rows.current));
      }
      frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [dragging]);

  function startDrag(id: string, e: PointerEvent<HTMLButtonElement>) {
    e.currentTarget.setPointerCapture(e.pointerId);
    pointerY.current = e.clientY;
    setDragging(id);
  }

  function drag(id: string, e: PointerEvent<HTMLButtonElement>) {
    if (dragging !== id) return;
    pointerY.current = e.clientY;
    setIds(current => dragTo(current, id, e.clientY, rows.current));
  }

  function nudge(id: string, e: KeyboardEvent<HTMLButtonElement>) {
    const by = e.key === 'ArrowUp' ? -1 : e.key === 'ArrowDown' ? 1 : 0;
    if (!by) return;
    e.preventDefault();
    const from = ids.indexOf(id);
    setIds(move(ids, from, from + by));
    // Moving the row can drop focus, so put it back on the same handle.
    requestAnimationFrame(() => handles.current.get(id)?.focus());
  }

  return (
    <>
      <div className='flex items-center gap-2 px-4 pb-2'>
        <p id={hintId} className='m-0 flex-1 text-sm text-muted'>
          Drag <FontAwesomeIcon icon={faGripLines} aria-label='the handles' /> to move items.
        </p>
        <button
          type='button'
          onClick={onCancel}
          disabled={saving}
          className='h-11 rounded-[10px] bg-chip px-4 text-soft disabled:opacity-50'
        >
          Cancel
        </button>
        <button
          type='button'
          onClick={() => onDone(ids)}
          disabled={saving}
          className='h-11 rounded-[10px] bg-primary px-4 font-medium text-on-primary disabled:opacity-50'
        >
          Done
        </button>
      </div>
      {!!error && <ErrorMessage error={error} />}

      <ul className='flex flex-col gap-2.5 px-4'>
        {ids.map(id => {
          const item = byId.get(id);
          if (!item) return null;
          return (
            <li
              key={id}
              ref={register(rows.current, id)}
            >
              <Card className={`flex items-center gap-1 pl-4 ${dragging === id ? 'ring-2 ring-primary' : ''}`}>
                <div className='flex min-w-0 flex-1 items-center gap-2.5 py-3.5'>
                  <OwnItemSummary item={item} />
                </div>
                <button
                  type='button'
                  ref={register(handles.current, id)}
                  aria-label={`Move ${item.name}`}
                  aria-describedby={hintId}
                  onPointerDown={e => startDrag(id, e)}
                  onPointerMove={e => drag(id, e)}
                  onPointerUp={() => setDragging(undefined)}
                  onPointerCancel={() => setDragging(undefined)}
                  onKeyDown={e => nudge(id, e)}
                  className='flex size-14 shrink-0 cursor-grab touch-none items-center justify-center text-faint active:cursor-grabbing'
                >
                  <FontAwesomeIcon icon={faGripLines} className='text-lg' aria-hidden='true' />
                </button>
              </Card>
            </li>
          );
        })}
      </ul>
    </>
  );
}
