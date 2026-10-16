import { faStar } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { type FormEvent, useId, useState } from 'react';
import type { OwnItem } from '../api';
import { useAddItem, useEditItem, useRemoveItem } from '../hooks';
import { BottomSheet } from './BottomSheet';
import { fieldsFrom, type ItemErrors, type ItemFields, itemSchema, saveErrors, validationErrors } from './itemForm';

type Props = {
  /** The item to edit, or null to add a new one. Undefined closes the sheet. */
  item: OwnItem | null | undefined;
  onClose: () => void;
};

export function ItemSheet({ item, onClose }: Props) {
  const titleId = useId();
  return (
    <BottomSheet open={item !== undefined} onClose={onClose} labelledBy={titleId}>
      {/* Keyed so each opening starts from the item's current values. */}
      {item !== undefined && <ItemForm key={item?.id ?? 'new'} item={item} titleId={titleId} onDone={onClose} />}
    </BottomSheet>
  );
}

function ItemForm({ item, titleId, onDone }: { item: OwnItem | null; titleId: string; onDone: () => void }) {
  const [fields, setFields] = useState(() => fieldsFrom(item));
  const [invalid, setInvalid] = useState<ItemErrors>({});
  const add = useAddItem();
  const edit = useEditItem();
  const remove = useRemoveItem();
  const busy = add.isMutating || edit.isMutating || remove.isMutating;
  const failed = add.error ?? edit.error;
  const errors: ItemErrors = {
    ...(failed && saveErrors(failed, 'Couldn’t save. Try again.')),
    ...(remove.error && saveErrors(remove.error, 'Couldn’t remove it. Try again.')),
    ...invalid,
  };

  const set = <K extends keyof ItemFields>(key: K, value: ItemFields[K]) => setFields(f => ({ ...f, [key]: value }));

  function save(e: FormEvent) {
    e.preventDefault();
    const result = itemSchema.safeParse(fields);
    if (!result.success) {
      setInvalid(validationErrors(result.error));
      return;
    }
    setInvalid({});
    if (item) void edit.trigger({ itemId: item.id, item: result.data }, { onSuccess: onDone });
    else void add.trigger(result.data, { onSuccess: onDone });
  }

  function removeItem() {
    if (!item || !window.confirm(`Remove “${item.name}” from your list?`)) return;
    void remove.trigger(item.id, { onSuccess: onDone });
  }

  const input = (field: keyof ItemErrors) =>
    `h-12 w-full rounded-xl border bg-card px-3.5 text-base text-ink outline-none focus:border-primary focus:border-[1.5px] ${
      errors[field] ? 'border-danger' : 'border-field'
    }`;

  return (
    <form onSubmit={save} noValidate className='flex flex-col gap-3.5'>
      <div className='flex items-center justify-between'>
        <button type='button' onClick={onDone} className='h-11 min-w-16 text-left text-base text-soft'>
          Cancel
        </button>
        <h2 id={titleId} className='m-0 font-display text-lg font-extrabold'>
          {item ? 'Edit item' : 'Add item'}
        </h2>
        <button
          type='submit'
          disabled={busy}
          className='h-11 min-w-16 text-right text-base font-bold text-primary disabled:opacity-50'
        >
          Save
        </button>
      </div>

      {errors.form && (
        <p role='alert' className='m-0 rounded-[10px] bg-accent-soft px-3.5 py-2.5 text-sm text-danger'>
          {errors.form}
        </p>
      )}

      <label className='flex flex-col gap-1.5 text-[13px] font-medium text-soft'>
        What is it?
        <input
          className={input('name')}
          value={fields.name}
          onChange={e => set('name', e.target.value)}
          placeholder='Wool hiking socks'
          maxLength={200}
          required
          autoFocus
        />
        {errors.name && <span className='text-danger'>{errors.name}</span>}
      </label>

      <label className='flex flex-col gap-1.5 text-[13px] font-medium text-soft'>
        Link (optional)
        <input
          className={input('link')}
          type='url'
          inputMode='url'
          value={fields.link}
          onChange={e => set('link', e.target.value)}
          placeholder='https://'
        />
        {errors.link && <span className='text-danger'>{errors.link}</span>}
      </label>

      <label className='flex flex-col gap-1.5 text-[13px] font-medium text-soft'>
        Price (optional)
        <input
          className={input('price')}
          inputMode='decimal'
          value={fields.price}
          onChange={e => set('price', e.target.value)}
          placeholder='$0.00'
        />
        {errors.price && <span className='text-danger'>{errors.price}</span>}
      </label>

      <label className='flex flex-col gap-1.5 text-[13px] font-medium text-soft'>
        Notes (optional)
        <textarea
          className={`${input('notes')} h-auto resize-none py-3`}
          rows={2}
          value={fields.notes}
          onChange={e => set('notes', e.target.value)}
          placeholder='Size, color, anything that helps'
          maxLength={2000}
        />
        {errors.notes && <span className='text-danger'>{errors.notes}</span>}
      </label>

      <button
        type='button'
        aria-pressed={fields.reallyWant}
        onClick={() => set('reallyWant', !fields.reallyWant)}
        className={`flex h-13 items-center gap-2.5 rounded-xl border px-3.5 text-left text-base ${
          fields.reallyWant ? 'border-accent bg-accent-soft' : 'border-field bg-card'
        }`}
      >
        <FontAwesomeIcon
          icon={faStar}
          className={`text-lg ${fields.reallyWant ? 'text-accent' : 'text-field'}`}
          aria-hidden='true'
        />
        I really want this
      </button>

      {item && (
        <button
          type='button'
          onClick={removeItem}
          disabled={busy}
          className='h-11 text-base font-medium text-danger disabled:opacity-50'
        >
          Remove from my list
        </button>
      )}
    </form>
  );
}
