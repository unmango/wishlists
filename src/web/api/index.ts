import createClient from 'openapi-fetch';
import type { components, paths } from './schema';
import * as auth from './auth';
import * as token from './token';

const baseUrl = import.meta.env.VITE_API_BASE_URL || '/';

export const client = createClient<paths>({ baseUrl });
client.use(auth.middleware);

export type Client = typeof client;
export type ProblemDetails = components['schemas']['HttpValidationProblemDetails'];
export type AccessTokenResponse = components['schemas']['AccessTokenResponse'];
export type User = components['schemas']['User'];
export { token };
