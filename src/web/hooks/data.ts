import useSWR, { mutate } from 'swr';
import { ApiError, defaultClient as client, type ItemRequest, send, unwrap } from '../api';

export const keys = {
  me: 'me',
  members: 'members',
  mine: 'mine',
  buying: 'buying',
  list: (id: string) => `list:${id}`,
};

/** The signed-in member, or null when nobody is signed in. */
export function useMe() {
  return useSWR(keys.me, async () => {
    try {
      return await unwrap(client.GET('/api/me'));
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) return null;
      throw e;
    }
  });
}

export function useMembers() {
  return useSWR(keys.members, () => unwrap(client.GET('/api/members')));
}

export function useMyWishlist() {
  return useSWR(keys.mine, () => unwrap(client.GET('/api/wishlists/mine')));
}

export function useWishlist(id: string) {
  return useSWR(keys.list(id), () => unwrap(client.GET('/api/wishlists/{id}', { params: { path: { id } } })));
}

export function useMyPurchases() {
  return useSWR(keys.buying, () => unwrap(client.GET('/api/purchases/mine')));
}

export async function addItem(item: ItemRequest) {
  await unwrap(client.POST('/api/wishlists/mine/items', { body: item }));
  await Promise.all([mutate(keys.mine), mutate(keys.members)]);
}

export async function editItem(itemId: string, item: ItemRequest) {
  await unwrap(client.PUT('/api/wishlists/mine/items/{itemId}', { params: { path: { itemId } }, body: item }));
  await Promise.all([mutate(keys.mine), mutate(keys.members)]);
}

export async function removeItem(itemId: string) {
  await send(client.DELETE('/api/wishlists/mine/items/{itemId}', { params: { path: { itemId } } }));
  await Promise.all([mutate(keys.mine), mutate(keys.members)]);
}

export async function renameWishlist(name: string) {
  await unwrap(client.PUT('/api/wishlists/mine', { body: { name } }));
  await mutate(keys.mine);
}

export async function markBought(wishlistId: string, itemId: string) {
  try {
    await send(
      client.PUT('/api/wishlists/{id}/items/{itemId}/bought', { params: { path: { id: wishlistId, itemId } } }),
    );
  } finally {
    // Refresh even on a conflict, so the list shows who got there first.
    await Promise.all([mutate(keys.list(wishlistId)), mutate(keys.buying)]);
  }
}

export async function unmarkBought(wishlistId: string, itemId: string) {
  try {
    await send(
      client.DELETE('/api/wishlists/{id}/items/{itemId}/bought', { params: { path: { id: wishlistId, itemId } } }),
    );
  } finally {
    await Promise.all([mutate(keys.list(wishlistId)), mutate(keys.buying)]);
  }
}

export async function signOut() {
  await fetch('/auth/logout', { method: 'POST' });
  await mutate(() => true, undefined, { revalidate: false });
  window.location.assign('/');
}
