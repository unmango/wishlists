import { faStar } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useId, useState } from 'react';
import type { OwnItem } from '../api';
import { formatPrice, linkHost } from '../format';
import { useImportItems } from '../hooks';
import { importRequest, type ImportRow, type PickedFile, planImport, type PlannedRow } from '../import/csvImport';
import { BottomSheet } from './BottomSheet';

type Props = {
  /** The picked file to preview. Undefined closes the sheet. */
  file: PickedFile | undefined;
  /** What's already on the wishlist, so those rows are skipped. */
  existing: OwnItem[];
  onClose: () => void;
};

export function ImportSheet({ file, existing, onClose }: Props) {
  const titleId = useId();
  return (
    <BottomSheet open={file !== undefined} onClose={onClose} labelledBy={titleId}>
      {file && (
        <>
          <div className='flex items-center justify-between'>
            <button type='button' onClick={onClose} className='h-11 min-w-16 text-left text-base text-soft'>
              Cancel
            </button>
            <h2 id={titleId} className='m-0 font-display text-lg font-extrabold'>Import</h2>
            <span className='min-w-16' />
          </div>
          {'error' in file
            ? (
              <p role='alert' className='m-0 rounded-[10px] bg-accent-soft px-3.5 py-2.5 text-sm text-danger'>
                {file.error}
              </p>
            )
            // Keyed so a new file starts fresh.
            : <Preview key={file.name} initial={file.rows} existing={existing} onDone={onClose} />}
        </>
      )}
    </BottomSheet>
  );
}

function Preview({ initial, existing, onDone }: { initial: ImportRow[]; existing: OwnItem[]; onDone: () => void }) {
  const [rows, setRows] = useState(initial);
  const importItems = useImportItems();
  const plan = planImport(rows, existing);
  const toAdd = importRequest(plan);
  const count = (status: PlannedRow['status']) => plan.filter(r => r.status === status).length;
  const duplicates = count('duplicate');
  const unnamed = count('needs-name');

  function rename(index: number, name: string) {
    setRows(current => current.map((row, i) => (i === index ? { ...row, name } : row)));
  }

  function add() {
    void importItems.trigger(toAdd, { onSuccess: onDone });
  }

  const summary = [
    `${toAdd.length} to add`,
    duplicates > 0 && `${duplicates} already on your wishlist`,
    unnamed > 0 && `${unnamed} ${unnamed === 1 ? 'needs' : 'need'} a name`,
  ].filter(Boolean).join(' · ');

  return (
    <>
      <p className='m-0 text-sm text-muted'>{summary}</p>

      {importItems.error && (
        <p role='alert' className='m-0 rounded-[10px] bg-accent-soft px-3.5 py-2.5 text-sm text-danger'>
          {importItems.error instanceof Error ? importItems.error.message : 'Couldn’t import. Try again.'}{' '}
          Nothing was added.
        </p>
      )}

      <ul className='-mx-5 flex max-h-[55svh] flex-col overflow-y-auto border-y border-line'>
        {plan.map((row, index) => (
          // The file's row order never changes, so the index is a stable key.
          <li key={index} className='border-b border-line px-5 py-3 last:border-b-0'>
            <PreviewRow
              row={row}
              // Rows that came without a name keep their field after it's typed, so focus stays put.
              editable={!initial[index]!.name}
              onRename={name => rename(index, name)}
            />
          </li>
        ))}
      </ul>

      <button
        type='button'
        onClick={add}
        disabled={toAdd.length === 0 || importItems.isMutating}
        className='h-12 rounded-xl bg-primary text-base font-medium text-on-primary disabled:opacity-50'
      >
        {importItems.isMutating ? 'Adding…' : `Add ${toAdd.length} ${toAdd.length === 1 ? 'item' : 'items'}`}
      </button>
    </>
  );
}

const statusLabels: Partial<Record<PlannedRow['status'], string>> = {
  duplicate: 'already on your wishlist',
  'needs-name': 'needs a name',
};

function PreviewRow(
  { row, editable, onRename }: { row: PlannedRow; editable: boolean; onRename: (name: string) => void },
) {
  const noteId = useId();
  const meta = [linkHost(row.link), formatPrice(row.price)].filter(Boolean).join(' · ');
  const notes = [statusLabels[row.status], ...row.warnings].filter(Boolean).join(' · ');
  const skipped = row.status === 'duplicate';

  return (
    <div className={`flex flex-col gap-1 ${skipped ? 'opacity-60' : ''}`}>
      {editable
        ? (
          <input
            value={row.name}
            onChange={e => onRename(e.target.value)}
            placeholder='Needs a name'
            aria-label={meta ? `Name for the item at ${meta}` : 'Name for this item'}
            aria-describedby={notes ? noteId : undefined}
            maxLength={200}
            className={`h-11 w-full rounded-xl border bg-card px-3.5 text-base text-ink outline-none focus:border-[1.5px] focus:border-primary ${
              row.status === 'needs-name' ? 'border-accent' : 'border-field'
            }`}
          />
        )
        : (
          <div className='flex items-center gap-1.5'>
            {row.reallyWant && (
              <FontAwesomeIcon icon={faStar} className='text-sm text-accent' aria-label='Really want' />
            )}
            <span className={`text-base font-medium break-words ${skipped ? 'line-through' : ''}`}>{row.name}</span>
          </div>
        )}
      {meta && <div className='truncate text-[13px] text-muted'>{meta}</div>}
      {notes && (
        <div id={noteId} className={`text-[13px] ${skipped ? 'text-muted' : 'text-danger'}`}>
          {notes}
        </div>
      )}
    </div>
  );
}
