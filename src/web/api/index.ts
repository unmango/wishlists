import createClient from 'openapi-fetch';
import { createQueryHook } from 'swr-openapi';
import type { components, paths } from './schema';

const baseUrl = import.meta.env.VITE_API_BASE_URL || '/';

export type Client = ReturnType<typeof createClient<paths>>;

export const defaultClient = createClient<paths>({ baseUrl });

export function client(_authToken: string): Client {
  const client = createClient<paths>({ baseUrl });
  // client.use(auth.middleware(authToken));
  return client;
}

const prefix = '/api';
export const useQuery = createQueryHook(defaultClient, prefix);

// export type ProblemDetails = components['schemas']['HttpValidationProblemDetails'];
export type Wishlist = components['schemas']['Wishlist'];
export type User = components['schemas']['User'];
