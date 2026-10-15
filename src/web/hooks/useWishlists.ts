import { useQuery } from '../api';

// Placeholder until the step 2 screens are designed.
export function useWishlists() {
  return useQuery('/api/wishlists/mine');
}
