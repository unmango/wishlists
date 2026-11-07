import { ApiError, useQuery } from '../api';

/** The signed-in member: null when nobody is signed in, undefined while loading. */
export function useMe() {
  const { data, error, isLoading } = useQuery('/api/me', undefined, { shouldRetryOnError: false });
  const signedOut = error instanceof ApiError && error.status === 401;
  return { me: signedOut ? null : data, error: signedOut ? undefined : error, isLoading };
}

export function useMembers() {
  return useQuery('/api/members');
}

export function useMyWishlist() {
  return useQuery('/api/wishlists/mine');
}

export function useWishlist(id: string) {
  return useQuery('/api/wishlists/{id}', { params: { path: { id } } });
}

export function useMyPurchases() {
  return useQuery('/api/purchases/mine');
}
