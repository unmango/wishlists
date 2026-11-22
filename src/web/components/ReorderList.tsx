import {
  type Announcements,
  closestCenter,
  DndContext,
  type DragEndEvent,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
} from '@dnd-kit/core';
import { restrictToParentElement, restrictToVerticalAxis } from '@dnd-kit/modifiers';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { faGripLines } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useState } from 'react';
import type { OwnItem } from '../api';
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

function Row({ item }: { item: OwnItem }) {
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } = useSortable({
    id: item.id,
  });

  return (
    <li
      ref={setNodeRef}
      style={{ transform: CSS.Translate.toString(transform), transition }}
      className={isDragging ? 'relative z-10' : ''}
    >
      <Card className={`flex items-center gap-1 pl-4 ${isDragging ? 'shadow-lg ring-2 ring-primary' : ''}`}>
        <div className='flex min-w-0 flex-1 items-center gap-2.5 py-3.5'>
          <OwnItemSummary item={item} />
        </div>
        <button
          type='button'
          ref={setActivatorNodeRef}
          {...attributes}
          {...listeners}
          aria-label={`Move ${item.name}`}
          className='flex size-14 shrink-0 cursor-grab touch-none items-center justify-center text-faint active:cursor-grabbing'
        >
          <FontAwesomeIcon icon={faGripLines} className='text-lg' aria-hidden='true' />
        </button>
      </Card>
    </li>
  );
}

/** The owner's wishlist with drag handles. Nothing is saved until Done. */
export function ReorderList({ items, saving, error, onDone, onCancel }: Props) {
  const [ids, setIds] = useState(() => items.map(x => x.id));
  const byId = new Map(items.map(x => [x.id, x]));
  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates }),
  );

  // Screen readers hear item names and places, not ids.
  const nameOf = (id: string | number) => byId.get(String(id))?.name ?? 'Item';
  const placeOf = (id: string | number) => `place ${ids.indexOf(String(id)) + 1} of ${ids.length}`;
  const announcements: Announcements = {
    onDragStart: ({ active }) => `Picked up ${nameOf(active.id)}, ${placeOf(active.id)}.`,
    onDragOver: ({ active, over }) => over ? `${nameOf(active.id)} moved to ${placeOf(over.id)}.` : undefined,
    onDragEnd: ({ active, over }) => over ? `Dropped ${nameOf(active.id)} at ${placeOf(over.id)}.` : undefined,
    onDragCancel: ({ active }) => `Put ${nameOf(active.id)} back.`,
  };

  function drop({ active, over }: DragEndEvent) {
    if (!over || active.id === over.id) return;
    setIds(current => arrayMove(current, current.indexOf(String(active.id)), current.indexOf(String(over.id))));
  }

  return (
    <>
      <div className='flex items-center gap-2 px-4 pb-2'>
        <p className='m-0 flex-1 text-sm text-muted'>
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

      <DndContext
        sensors={sensors}
        collisionDetection={closestCenter}
        modifiers={[restrictToVerticalAxis, restrictToParentElement]}
        accessibility={{ announcements }}
        onDragEnd={drop}
      >
        <SortableContext items={ids} strategy={verticalListSortingStrategy}>
          <ul className='flex flex-col gap-2.5 px-4'>
            {ids.map(id => {
              const item = byId.get(id);
              return item && <Row key={id} item={item} />;
            })}
          </ul>
        </SortableContext>
      </DndContext>
    </>
  );
}
