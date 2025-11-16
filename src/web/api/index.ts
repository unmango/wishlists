import createClient from 'openapi-fetch';
import * as auth from './auth';
import type { components, paths } from './schema';

const baseUrl = import.meta.env.VITE_API_BASE_URL || '/';

export type Client = ReturnType<typeof createClient<paths>>;

export const defaultClient = createClient<paths>({ baseUrl });

export function client(authToken: string): Client {
  const client = createClient<paths>({ baseUrl });
  client.use(auth.middleware(authToken));
  return client;
}

// export type ProblemDetails = components['schemas']['HttpValidationProblemDetails'];
export type User = components['schemas']['User'];
export { auth };
