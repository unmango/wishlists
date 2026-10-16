import { faStar } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { type FormEvent, useEffect, useId, useRef, useState } from 'react';
import { ApiError, type ItemRequest, type OwnItem, toNumber } from '../api';
import { addItem, editItem, removeItem } from '../hooks';

type Props = {
  /** The item to edit, or null to add a new one. Undefined closes the sheet. */
  item: OwnItem | null | undefined;
  onClose: () => void;
};

type Fields = { name: string; link: string; price: string; notes: string; reallyWant: boolean };

const empty: Fields = { name: '', link: '', price: '', notes: '', reallyWant: false };

function fromItem(item: OwnItem | null): Fields {
  if (!item) return empty;
  const price = toNumber(item.price);
  return {
    name: item.name,
    link: item.link ?? '',
    price: price === undefined ? '' : String(price),
    notes: item.notes ?? '',
    reallyWant: item.reallyWant,
  };
}

function toRequest(fields: Fields): ItemRequest | Record<string, string> {
  const priceText = fields.price.replace(/[$,\s]/g, '');
  const price = priceText === '' ? null : Number(priceText);
  if (price !== null && !Number.isFinite(price)) return { price: 'Enter a price like 24 or 24.99.' };

  return {
    name: fields.name.trim(),
    link: fields.link.trim() || null,
    price,
    notes: fields.notes.trim() || null,
    reallyWant: fields.reallyWant,
  };
}

export function ItemSheet({ item, onClose }: Props) {
  const dialog = useRef<HTMLDialogElement>(null);
  const open = item !== undefined;
  const editing = item ?? null;
  const [fields, setFields] = useState<Fields>(empty);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const id = useId();

  useEffect(() => {
    const el = dialog.current;
    if (!el) return;
    if (open && !el.open) {
      setFields(fromItem(editing));
      setErrors({});
      el.showModal();
    } else if (!open && el.open) {
      el.close();
    }
  }, [open, editing]);

  const set = <K extends keyof Fields>(key: K, value: Fields[K]) => setFields(f => ({ ...f, [key]: value }));

  async function save(e: FormEvent) {
    e.preventDefault();
    const request = toRequest(fields);
    if (!('reallyWant' in request)) {
      setErrors(request);
      return;
    }

    setBusy(true);
    try {
      if (editing) await editItem(editing.id, request as ItemRequest);
      else await addItem(request as ItemRequest);
      onClose();
    } catch (err) {
      setErrors(
        err instanceof ApiError ? { form: err.message, ...err.fieldErrors } : { form: 'Couldn’t save. Try again.' },
      );
    } finally {
      setBusy(false);
    }
  }

  async function remove() {
    if (!editing || !window.confirm(`Remove “${editing.name}” from your list?`)) return;
    setBusy(true);
    try {
      await removeItem(editing.id);
      onClose();
    } catch (err) {
      setErrors({ form: err instanceof Error ? err.message : 'Couldn’t remove it. Try again.' });
    } finally {
      setBusy(false);
    }
  }

  const input = (field: keyof typeof errors) =>
    `h-12 w-full rounded-xl border bg-card px-3.5 text-base text-ink outline-none focus:border-primary focus:border-[1.5px] ${
      errors[field] ? 'border-danger' : 'border-field'
    }`;

  return (
    <dialog
      ref={dialog}
      onClose={onClose}
      aria-labelledby={`${id}-title`}
      className='m-0 mt-auto max-h-[92svh] w-full max-w-none bg-transparent p-0 backdrop:bg-black/45 sm:mx-auto sm:max-w-md'
    >
      <form
        onSubmit={save}
        noValidate
        className='flex flex-col gap-3.5 rounded-t-3xl bg-card px-5 pt-2.5 pb-[max(env(safe-area-inset-bottom),24px)] text-ink'
      >
        <div aria-hidden='true' className='h-1.5 w-10 self-center rounded-full bg-field' />
        <div className='flex items-center justify-between'>
          <button type='button' onClick={onClose} className='h-11 min-w-16 text-left text-base text-soft'>
            Cancel
          </button>
          <h2 id={`${id}-title`} className='m-0 font-display text-lg font-extrabold'>
            {editing ? 'Edit item' : 'Add item'}
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

        {editing && (
          <button
            type='button'
            onClick={remove}
            disabled={busy}
            className='h-11 text-base font-medium text-danger disabled:opacity-50'
          >
            Remove from my list
          </button>
        )}
      </form>
    </dialog>
  );
}
