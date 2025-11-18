import createClient from 'openapi-fetch';
import { createMutateHook, createQueryHook } from 'swr-openapi';
import type { components, paths } from './schema';

const baseUrl = import.meta.env.VITE_API_BASE_URL || '/';

export type Client = ReturnType<typeof createClient<paths>>;
export const defaultClient = createClient<paths>({ baseUrl });

const prefix = '/api'; // The slash is an aesthetic choice, it has no effect
export const useQuery = createQueryHook(defaultClient, prefix);
export const useMutate = createMutateHook(defaultClient, prefix, Bun.deepEquals);

// export type ProblemDetails = components['schemas']['HttpValidationProblemDetails'];
export type Wishlist = components['schemas']['Wishlist'];
export type User = components['schemas']['User'];
