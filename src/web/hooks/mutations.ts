import { useSWRConfig } from 'swr';
import useSWRMutation from 'swr/mutation';
import { defaultClient as client, type ItemRequest, type OwnItem, useMutate } from '../api';

// Errors land in the hook's `error` instead of rejecting trigger().
const options = { throwOnError: false } as const;

/** Refreshes what a change to my own list affects: the list itself and the family item counts. */
function useRefreshMine() {
  const mutate = useMutate();
  return () => Promise.all([mutate(['/api/wishlists/mine']), mutate(['/api/members'])]);
}

/**
 * A new link's thumbnail is looked for in the background after the save, so refresh my list once
 * more after it has had time to land. Otherwise it shows up on the next normal refresh.
 */
const thumbnailLookupMs = 3000;

function useRefreshMineForThumbnail() {
  const mutate = useMutate();
  return (saved: OwnItem | undefined) => {
    if (saved?.link && !saved.thumbnail) setTimeout(() => void mutate(['/api/wishlists/mine']), thumbnailLookupMs);
  };
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
    // Same-link edits keep their thumbnail, so in practice this fires for a new link.
    refreshForThumbnail(data);
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
    const response = await fetch('/auth/logout', { method: 'POST' });
    if (!response.ok) throw new Error('Couldn’t sign out. Try again.');
    await mutate(() => true, undefined, { revalidate: false });
    window.location.assign('/');
  }, options);
}
