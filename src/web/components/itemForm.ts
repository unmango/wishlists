import { z } from 'zod';
import { ApiError, type ItemRequest, type OwnItem } from '../api';
import { toNumber } from '../format';

/** What the item form's inputs hold, all as typed. */
export type ItemFields = { name: string; link: string; price: string; notes: string; reallyWant: boolean };

/** One message per field, plus `form` for anything that isn't about a single field. */
export type ItemErrors = Partial<Record<keyof ItemFields | 'form', string>>;

const fieldNames = ['name', 'link', 'price', 'notes', 'reallyWant'] as const satisfies (keyof ItemFields)[];

function isWebLink(value: string): boolean {
  try {
    return ['http:', 'https:'].includes(new URL(value).protocol);
  } catch {
    return false;
  }
}

/** Turns what was typed into an ItemRequest. Mirrors the API's own checks so most mistakes never leave the phone. */
export const itemSchema = z.object({
  name: z.string().trim().min(1, 'Give it a name.').max(200, 'Keep the name under 200 characters.'),
  link: z
    .string()
    .trim()
    .refine(v => v === '' || (v.length <= 2000 && isWebLink(v)), 'Enter a link that starts with https://.')
    .transform(v => v || null),
  price: z
    .string()
    .transform(v => v.replace(/[$,\s]/g, ''))
    // A plain decimal only: Number() would also take '0x10', '1e3' and 'Infinity'.
    .refine(v => v === '' || /^\d+(\.\d{1,2})?$/.test(v), 'Enter a price like 24 or 24.99.')
    .transform(v => (v === '' ? null : Number(v))),
  notes: z.string().trim().max(2000, 'Keep notes under 2,000 characters.').transform(v => v || null),
  reallyWant: z.boolean(),
}) satisfies z.ZodType<ItemRequest, ItemFields>;

export const emptyFields: ItemFields = { name: '', link: '', price: '', notes: '', reallyWant: false };

export function fieldsFrom(item: OwnItem | null): ItemFields {
  if (!item) return emptyFields;
  const price = toNumber(item.price);
  return {
    name: item.name,
    link: item.link ?? '',
    price: price === undefined ? '' : String(price),
    notes: item.notes ?? '',
    reallyWant: item.reallyWant,
  };
}

export function validationErrors(error: z.ZodError<z.infer<typeof itemSchema>>): ItemErrors {
  const { fieldErrors } = z.flattenError(error);
  return Object.fromEntries(fieldNames.flatMap(f => (fieldErrors[f]?.[0] ? [[f, fieldErrors[f][0]]] : [])));
}

/** Errors from a failed save, with the API's field errors mapped onto the form's fields. */
export function saveErrors(error: unknown, fallback: string): ItemErrors {
  if (!(error instanceof ApiError)) return { form: fallback };
  const fields = error.fieldErrors;
  return {
    form: error.message,
    ...Object.fromEntries(fieldNames.flatMap(f => (fields[f.toLowerCase()] ? [[f, fields[f.toLowerCase()]]] : []))),
  };
}
