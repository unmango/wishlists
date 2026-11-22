import { useState } from 'react';
import { type Sort, toSort } from '../sorting/sorts';

const key = 'wishlists.sort';

// Storage can be missing or throw (private windows, blocked site data): then the sort just isn't remembered.
function read(): Sort {
  try {
    return toSort(localStorage.getItem(key));
  } catch {
    return 'order';
  }
}

function write(sort: Sort) {
  try {
    localStorage.setItem(key, sort);
  } catch {
    // Not remembered, but still used for this visit.
  }
}

/** The member's sort, remembered on this device. One setting for every wishlist, never saved to the account. */
export function useSort() {
  const [sort, setSort] = useState(read);
  return [sort, (next: Sort) => {
    setSort(next);
    write(next);
  }] as const;
}
