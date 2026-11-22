import { toNumber } from '../format';

/** How a member chooses to show a wishlist. A sort never changes the owner's order. */
export const sorts = ['order', 'reallyWant', 'priceHigh', 'priceLow'] as const;
export type Sort = (typeof sorts)[number];

type Sortable = { reallyWant: boolean; price?: number | string | null };

export function toSort(value: unknown): Sort {
  return sorts.find(x => x === value) ?? 'order';
}

/** The menu label. `ownerName` is the first name of the owner, or undefined on your own wishlist. */
export function sortLabel(sort: Sort, ownerName: string | undefined): string {
  switch (sort) {
    case 'order':
      return ownerName ? `${ownerName}’s order` : 'My order';
    case 'reallyWant':
      return 'Really want first';
    case 'priceHigh':
      return 'Price high to low';
    case 'priceLow':
      return 'Price low to high';
  }
}

function byPrice(direction: 1 | -1) {
  return (a: Sortable, b: Sortable) => {
    const x = toNumber(a.price);
    const y = toNumber(b.price);
    if (x === undefined || y === undefined) return (x === undefined ? 1 : 0) - (y === undefined ? 1 : 0);
    return (x - y) * direction;
  };
}

const compare: Record<Exclude<Sort, 'order'>, (a: Sortable, b: Sortable) => number> = {
  reallyWant: (a, b) => Number(b.reallyWant) - Number(a.reallyWant),
  priceHigh: byPrice(-1),
  priceLow: byPrice(1),
};

/** A sorted copy. The sort is stable, so items it treats as equal keep the owner's order. */
export function sortItems<T extends Sortable>(items: readonly T[], sort: Sort): T[] {
  return sort === 'order' ? [...items] : [...items].sort(compare[sort]);
}

/** A copy of `list` with the entry at `from` moved to `to`. */
export function move<T>(list: readonly T[], from: number, to: number): T[] {
  const next = [...list];
  const [entry] = next.splice(from, 1);
  if (entry === undefined) return next;
  next.splice(Math.max(0, Math.min(to, next.length)), 0, entry);
  return next;
}
