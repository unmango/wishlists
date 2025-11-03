import createClient from 'openapi-fetch';
import type { components, paths } from './schema';

const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5217';

export const client = createClient<paths>({ baseUrl });

export type Client = typeof client;
export type ProblemDetails = components['schemas']['HttpValidationProblemDetails'];
export type AccessTokenResponse = components['schemas']['AccessTokenResponse'];
export type User = components['schemas']['User'];
