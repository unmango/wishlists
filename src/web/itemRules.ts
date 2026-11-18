/** The limits on an item's fields. The API checks the same ones. */
export const itemLimits = { name: 200, link: 2000, notes: 2000 } as const;

export function isWebLink(value: string): boolean {
  try {
    return ['http:', 'https:'].includes(new URL(value).protocol);
  } catch {
    return false;
  }
}

/** Drops the dollar signs, thousands commas and spaces people type around a price. */
export const cleanPrice = (value: string) => value.replace(/[$,\s]/g, '');

/**
 * A plain decimal only: Number() would also take '0x10', '1e3' and 'Infinity'.
 * Capped well below what the API's decimal can hold, so a typo can't fail a save.
 */
export const isPlainPrice = (value: string) => /^\d{1,9}(\.\d{1,2})?$/.test(value);
