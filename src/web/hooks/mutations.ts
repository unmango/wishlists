import { useSWRConfig } from 'swr';
import useSWRMutation from 'swr/mutation';
import { defaultClient as client, type ItemRequest, useMutate } from '../api';

// Errors land in the hook's `error` instead of rejecting trigger().
const options = { throwOnError: false } as const;

/** Refreshes what a change to my own list affects: the list itself and the family item counts. */
function useRefreshMine() {
  const mutate = useMutate();
  return () => Promise.all([mutate(['/api/wishlists/mine']), mutate(['/api/members'])]);
}

/** Refreshes someone else's list and my purchases after a mark changes. */
function useRefreshMarks(wishlistId: string) {
  const mutate = useMutate();
  return () =>
    Promise.all([
      mutate(['/api/wishlists/{id}', { params: { path: { id: wishlistId } } }]),
      mutate(['/api/purchases/mine']),
    ]);
}

export function useAddItem() {
  const refresh = useRefreshMine();
  return useSWRMutation('add-item', async (_, { arg }: { arg: ItemRequest }) => {
    await client.POST('/api/wishlists/mine/items', { body: arg });
    await refresh();
  }, options);
}

export function useEditItem() {
  const refresh = useRefreshMine();
  return useSWRMutation('edit-item', async (_, { arg }: { arg: { itemId: string; item: ItemRequest } }) => {
    const { itemId, item } = arg;
    await client.PUT('/api/wishlists/mine/items/{itemId}', { params: { path: { itemId } }, body: item });
    await refresh();
  }, options);
}

export function useRemoveItem() {
  const refresh = useRefreshMine();
  return useSWRMutation('remove-item', async (_, { arg: itemId }: { arg: string }) => {
    await client.DELETE('/api/wishlists/mine/items/{itemId}', { params: { path: { itemId } } });
    await refresh();
  }, options);
}

export function useRenameWishlist() {
  const refresh = useRefreshMine();
  return useSWRMutation('rename-wishlist', async (_, { arg }: { arg: string }) => {
    await client.PUT('/api/wishlists/mine', { body: { name: arg } });
    await refresh();
  }, options);
}

export function useMarkBought(wishlistId: string, itemId: string) {
  const refresh = useRefreshMarks(wishlistId);
  return useSWRMutation(['mark-bought', itemId], async () => {
    try {
      await client.PUT('/api/wishlists/{id}/items/{itemId}/bought', { params: { path: { id: wishlistId, itemId } } });
    } finally {
      // Refresh even on a conflict, so the list shows who got there first.
      await refresh();
    }
  }, options);
}

export function useUnmarkBought(wishlistId: string, itemId: string) {
  const refresh = useRefreshMarks(wishlistId);
  return useSWRMutation(['unmark-bought', itemId], async () => {
    try {
      await client.DELETE('/api/wishlists/{id}/items/{itemId}/bought', {
        params: { path: { id: wishlistId, itemId } },
      });
    } finally {
      await refresh();
    }
  }, options);
}

export function useSignOut() {
  const { mutate } = useSWRConfig();
  return useSWRMutation('sign-out', async () => {
    await fetch('/auth/logout', { method: 'POST' });
    await mutate(() => true, undefined, { revalidate: false });
    window.location.assign('/');
  }, options);
}
