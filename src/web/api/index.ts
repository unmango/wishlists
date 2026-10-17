import createClient, { type Middleware } from 'openapi-fetch';
import { createMutateHook, createQueryHook } from 'swr-openapi';
import type { components, paths } from './schema';

const baseUrl = import.meta.env.VITE_API_BASE_URL || '/';

export type Me = components['schemas']['Me'];
export type Member = components['schemas']['Member'];
export type OwnWishlist = components['schemas']['OwnWishlist'];
export type OwnItem = components['schemas']['OwnItem'];
export type GiftWishlist = components['schemas']['GiftWishlist'];
export type GiftItem = components['schemas']['GiftItem'];
export type ItemRequest = components['schemas']['ItemRequest'];
export type MyPurchase = components['schemas']['MyPurchase'];

/** An API call that didn't succeed. */
export class ApiError extends Error {
  readonly status: number;
  readonly body: unknown;

  constructor(status: number, body: unknown) {
    super(problemTitle(body) ?? `Request failed (${status})`);
    this.status = status;
    this.body = body;
  }

  /** Field errors from a validation problem, keyed by lower-case field name. */
  get fieldErrors(): Record<string, string> {
    const errors = (this.body as { errors?: Record<string, string[]> } | undefined)?.errors ?? {};
    return Object.fromEntries(Object.entries(errors).map(([k, v]) => [k.toLowerCase(), v[0] ?? '']));
  }
}

function problemTitle(body: unknown): string | undefined {
  const title = (body as { title?: unknown } | undefined)?.title;
  return typeof title === 'string' ? title : undefined;
}

/** Turns every non-2xx response into a thrown ApiError, so callers only see data. */
const throwOnError: Middleware = {
  async onResponse({ response }) {
    if (response.ok) return undefined;
    const body: unknown = await response.clone().json().catch(() => undefined);
    throw new ApiError(response.status, body);
  },
};

export type Client = ReturnType<typeof createClient<paths>>;
export const defaultClient = createClient<paths>({ baseUrl });
defaultClient.use(throwOnError);

/** True when every value in partial is also in value, recursively. */
function isMatch(value: unknown, partial: unknown): boolean {
  if (partial === null || typeof partial !== 'object') return Object.is(value, partial);
  if (value === null || typeof value !== 'object') return false;
  return Object.entries(partial).every(([k, v]) => isMatch((value as Record<string, unknown>)[k], v));
}

const prefix = '/api'; // The slash is an aesthetic choice, it has no effect
// Errors from the hook are whatever throwOnError threw.
export const useQuery = createQueryHook<paths, `${string}/${string}`, typeof prefix, ApiError>(defaultClient, prefix);
export const useMutate = createMutateHook(defaultClient, prefix, isMatch);
