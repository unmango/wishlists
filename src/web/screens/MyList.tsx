import { faFileImport, faPen, faPlus } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { type ChangeEvent, type FormEvent, useRef, useState } from 'react';
import type { OwnItem } from '../api';
import { ImportSheet } from '../components/ImportSheet';
import { ItemSheet } from '../components/ItemSheet';
import { Card, ErrorMessage, Loading, PageHeader, ReallyWantStar, Screen, Thumbnail } from '../components/ui';
import { formatPrice, linkHost } from '../format';
import { useMyWishlist, useRenameWishlist } from '../hooks';
import { type PickedFile, readCsvFile } from '../import/csvImport';

function itemMeta(item: OwnItem): string {
  const parts = [linkHost(item.link) ?? 'No link'];
  if (item.notes) parts.push(item.notes.split('\n')[0]!);
  return parts.join(' · ');
}

function RenameForm({ current, onDone }: { current: string; onDone: () => void }) {
  const [name, setName] = useState(current);
  const rename = useRenameWishlist();

  function submit(e: FormEvent) {
    e.preventDefault();
    void rename.trigger(name.trim(), { onSuccess: onDone });
  }

  return (
    <form onSubmit={submit} className='flex flex-col gap-2 px-5 pt-14 pb-4'>
      <label htmlFor='list-name' className='text-sm text-muted'>
        List name
      </label>
      <input
        id='list-name'
        value={name}
        onChange={e => setName(e.target.value)}
        maxLength={200}
        autoFocus
        className='h-12 rounded-xl border-[1.5px] border-primary bg-card px-3.5 font-display text-2xl font-extrabold text-ink outline-none'
      />
      {rename.error && (
        <p role='alert' className='m-0 text-sm text-danger'>
          {rename.error instanceof Error ? rename.error.message : 'Couldn’t rename it.'}
        </p>
      )}
      <div className='flex gap-2'>
        <button
          type='submit'
          disabled={rename.isMutating}
          className='h-11 rounded-[10px] bg-primary px-4 font-medium text-on-primary'
        >
          Save
        </button>
        <button type='button' onClick={onDone} className='h-11 rounded-[10px] bg-chip px-4 text-soft'>
          Cancel
        </button>
      </div>
    </form>
  );
}

export function MyList() {
  const { data, error, isLoading } = useMyWishlist();
  // undefined: sheet closed. null: adding. An item: editing it.
  const [sheetItem, setSheetItem] = useState<OwnItem | null | undefined>(undefined);
  const [renaming, setRenaming] = useState(false);
  const [picked, setPicked] = useState<PickedFile>();
  const filePicker = useRef<HTMLInputElement>(null);

  async function pickFile(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    // Cleared so picking the same file again (say, after fixing it) still fires a change.
    e.target.value = '';
    if (file) setPicked(await readCsvFile(file));
  }

  return (
    <Screen>
      {renaming && data ? <RenameForm current={data.name} onDone={() => setRenaming(false)} /> : (
        <PageHeader
          eyebrow='My list'
          title={data?.name ?? 'My wishlist'}
          action={data && (
            <>
              <button
                type='button'
                onClick={() => filePicker.current?.click()}
                className='flex h-11 shrink-0 items-center gap-2 rounded-full bg-card px-4 text-soft'
              >
                <FontAwesomeIcon icon={faFileImport} aria-hidden='true' />
                Import
              </button>
              <button
                type='button'
                aria-label='Rename list'
                onClick={() => setRenaming(true)}
                className='flex size-11 shrink-0 items-center justify-center rounded-full bg-card text-soft'
              >
                <FontAwesomeIcon icon={faPen} aria-hidden='true' />
              </button>
            </>
          )}
        />
      )}

      {isLoading && <Loading />}
      {error && <ErrorMessage error={error} />}
      {data && data.items.length === 0 && (
        <p className='px-5 text-muted'>
          Your list is empty. Add the first thing you’d like, or import a list you already keep as a CSV file.
        </p>
      )}

      <ul className='flex flex-col gap-2.5 px-4'>
        {data?.items.map(item => (
          <li key={item.id}>
            <Card>
              <button
                type='button'
                onClick={() => setSheetItem(item)}
                className='flex w-full items-center gap-2.5 px-4 py-3.5 text-left text-ink'
              >
                <Thumbnail src={item.thumbnail} />
                <div className='flex min-w-0 flex-1 flex-col gap-0.5'>
                  <div className='flex items-center gap-1.5'>
                    {item.reallyWant && <ReallyWantStar label='Really want' />}
                    <span className='text-base font-medium break-words'>{item.name}</span>
                  </div>
                  <div className='truncate text-[13px] text-muted'>{itemMeta(item)}</div>
                </div>
                <div className='text-[15px] font-bold'>{formatPrice(item.price)}</div>
              </button>
            </Card>
          </li>
        ))}
      </ul>

      {data && data.items.length > 0 && (
        <p className='mx-5 mt-3 text-[13px] leading-snug text-muted'>
          You won’t see what’s been bought. Tap an item to edit or remove it.
        </p>
      )}

      <button
        type='button'
        onClick={() => setSheetItem(null)}
        className='fixed right-[max(20px,calc(50vw-204px))] bottom-24 flex h-14 items-center gap-2 rounded-xl bg-primary px-5.5 text-base font-medium text-on-primary shadow-[0_6px_16px_rgba(0,0,0,0.25)]'
      >
        <FontAwesomeIcon icon={faPlus} aria-hidden='true' />
        Add item
      </button>

      <ItemSheet item={sheetItem} onClose={() => setSheetItem(undefined)} />

      <input
        ref={filePicker}
        type='file'
        // The MIME types help Android pickers that grey out files the extension alone doesn't match.
        accept='.csv,text/csv,text/comma-separated-values'
        onChange={e => void pickFile(e)}
        hidden
      />
      <ImportSheet file={picked} existing={data?.items ?? []} onClose={() => setPicked(undefined)} />
    </Screen>
  );
}
