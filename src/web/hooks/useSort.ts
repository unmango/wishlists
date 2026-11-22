import { useState } from 'react';
import { type Sort, toSort } from '../sorting/sorts';

const key = 'wishlists.sort';

// Private windows and blocked site data make storage throw an error: then this just doesn't remember the sort.
function read(): Sort {
  try {
    return toSort(localStorage.getItem(key));
  } catch (error) {
    console.debug('Couldn’t read the saved sort, so using the owner’s order.', error);
    return 'order';
  }
}

function write(sort: Sort) {
  try {
    localStorage.setItem(key, sort);
  } catch (error) {
    // This visit still uses the sort; the next one won't remember it.
    console.debug('Couldn’t save the sort on this device.', error);
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
