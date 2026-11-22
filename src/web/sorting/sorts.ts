import { toNumber } from '../format';

type Sortable = { reallyWant: boolean; price?: number | string | null };
type Compare = (a: Sortable, b: Sortable) => number;

function byPrice(direction: 1 | -1) {
  return (a: Sortable, b: Sortable) => {
    const x = toNumber(a.price);
    const y = toNumber(b.price);
    if (x === undefined || y === undefined) return (x === undefined ? 1 : 0) - (y === undefined ? 1 : 0);
    return (x - y) * direction;
  };
}

/** How a member chooses to show a wishlist, in menu order. A sort never changes the owner's order. */
export const sorts = ['order', 'reallyWant', 'priceHigh', 'priceLow'] as const;
export type Sort = (typeof sorts)[number];

/** `order` has no comparer or fixed label: it keeps the owner's order unchanged and takes the owner's name. */
const choices: Record<Sort, { label: string | undefined; compare: Compare | undefined }> = {
  order: { label: undefined, compare: undefined },
  reallyWant: { label: 'Really want first', compare: (a, b) => Number(b.reallyWant) - Number(a.reallyWant) },
  priceHigh: { label: 'Price high to low', compare: byPrice(-1) },
  priceLow: { label: 'Price low to high', compare: byPrice(1) },
};

export function toSort(value: unknown): Sort {
  return sorts.find(x => x === value) ?? 'order';
}

/** The menu label. `ownerName` is the first name of the owner, or undefined on your own wishlist. */
export function sortLabel(sort: Sort, ownerName: string | undefined): string {
  return choices[sort].label ?? (ownerName ? `${ownerName}’s order` : 'My order');
}

/** A sorted copy. The sort is stable, so items it treats as equal keep the owner's order. */
export function sortItems<T extends Sortable>(items: readonly T[], sort: Sort): T[] {
  const compare = choices[sort].compare;
  return compare ? [...items].sort(compare) : [...items];
}
