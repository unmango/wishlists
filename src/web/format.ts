/** .NET serializes decimals as number or string; this always gives a number. */
export function toNumber(value: number | string | null | undefined): number | undefined {
  if (value === null || value === undefined || value === '') return undefined;
  const n = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(n) ? n : undefined;
}

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 });

export function formatPrice(value: number | string | null | undefined): string {
  const n = toNumber(value);
  if (n === undefined) return '';
  return money.format(n).replace(/\.00$/, '');
}

/** The site's host name, for showing under an item ("rei.com"). */
export function linkHost(link: string | null | undefined): string | undefined {
  if (!link) return undefined;
  try {
    return new URL(link).hostname.replace(/^www\./, '');
  } catch {
    return undefined;
  }
}
