import { useCallback } from 'react';
import { useQuery } from '../api';
import { useApi } from './useApi';

export function useWishlists() {
  const client = useApi();
  const { data, error, isLoading } = useQuery('/api/wishlists');

  const create = useCallback(async (name: string, ownerId: string) => {
    await client.POST('/api/wishlists', {
      body: { name, ownerId },
    });
  }, [client]);

  return {
    create,
    data,
    error,
    isLoading,
  };
}
