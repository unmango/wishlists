import { useSWRConfig } from 'swr';
import useSWRMutation from 'swr/mutation';
import { defaultClient as client, type ItemRequest, type OwnItem, useMutate } from '../api';

// The hook's `error` holds errors instead of trigger() throwing them.
const options = { throwOnError: false } as const;

/** Refreshes what a change to my own list affects: the list itself and the family item counts. */
function useRefreshMine() {
  const mutate = useMutate();
  return () => Promise.all([mutate(['/api/wishlists/mine']), mutate(['/api/members'])]);
}

/**
 * The server looks for a new link's thumbnail in the background after the save, so refresh my list
 * once more after giving it time. Otherwise the next normal refresh picks it up.
 */
const thumbnailLookupMs = 3000;

function useRefreshMineForThumbnail() {
  const mutate = useMutate();
  return (...saved: (OwnItem | undefined)[]) => {
    if (saved.some(x => x?.link && !x.thumbnail)) {
      setTimeout(() => void mutate(['/api/wishlists/mine']), thumbnailLookupMs);
    }
  };
}

/** Refreshes someone else's list and my purchases after a change to a mark. */
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
  const refreshForThumbnail = useRefreshMineForThumbnail();
  return useSWRMutation('add-item', async (_, { arg }: { arg: ItemRequest }) => {
    const { data } = await client.POST('/api/wishlists/mine/items', { body: arg });
    await refresh();
    refreshForThumbnail(data);
  }, options);
}

export function useEditItem() {
  const refresh = useRefreshMine();
  const refreshForThumbnail = useRefreshMineForThumbnail();
  return useSWRMutation('edit-item', async (_, { arg }: { arg: { itemId: string; item: ItemRequest } }) => {
    const { itemId, item } = arg;
    const { data } = await client.PUT('/api/wishlists/mine/items/{itemId}', {
      params: { path: { itemId } },
      body: item,
    });
    await refresh();
    // Same-link edits keep their thumbnail, so in practice only a new link triggers this.
    refreshForThumbnail(data);
  }, options);
}

/** Adds many items at once. All or nothing: a failed import adds no items. */
export function useImportItems() {
  const refresh = useRefreshMine();
  const refreshForThumbnail = useRefreshMineForThumbnail();
  return useSWRMutation('import-items', async (_, { arg: items }: { arg: ItemRequest[] }) => {
    const { data } = await client.POST('/api/wishlists/mine/items/import', { body: { items } });
    await refresh();
    refreshForThumbnail(...(data?.added ?? []));
    return data;
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
      // Refresh even on a conflict, so the list shows who claimed the item first.
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
    const response = await fetch('/auth/logout', { method: 'POST' });
    if (!response.ok) throw new Error('Couldn’t sign you out. Try again.');
    await mutate(() => true, undefined, { revalidate: false });
    window.location.assign('/');
  }, options);
}
