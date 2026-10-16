import createClient from 'openapi-fetch';
import { createQueryHook } from 'swr-openapi';
import type { components, paths } from './schema';

const baseUrl = import.meta.env.VITE_API_BASE_URL || '/';

export type Client = ReturnType<typeof createClient<paths>>;
export const defaultClient = createClient<paths>({ baseUrl });

const prefix = '/api'; // The slash is an aesthetic choice, it has no effect
export const useQuery = createQueryHook(defaultClient, prefix);

// export type ProblemDetails = components['schemas']['HttpValidationProblemDetails'];
export type Me = components['schemas']['Me'];
export type Member = components['schemas']['Member'];
export type OwnWishlist = components['schemas']['OwnWishlist'];
export type OwnItem = components['schemas']['OwnItem'];
export type GiftWishlist = components['schemas']['GiftWishlist'];
export type GiftItem = components['schemas']['GiftItem'];
export type ItemRequest = components['schemas']['ItemRequest'];
export type MyPurchase = components['schemas']['MyPurchase'];

/** An API call that didn't return the expected body. */
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

/** Unwraps an openapi-fetch result, throwing ApiError when there's no data. */
export async function unwrap<T>(
  request: Promise<{ data?: T; error?: unknown; response: Response }>,
): Promise<T> {
  const { data, error, response } = await request;
  if (!response.ok || data === undefined) throw new ApiError(response.status, error);
  return data;
}

/** Like unwrap, for calls that return no body (204). */
export async function send(request: Promise<{ error?: unknown; response: Response }>): Promise<void> {
  const { error, response } = await request;
  if (!response.ok) throw new ApiError(response.status, error);
}

/** .NET serializes decimals as number or string; this always gives a number. */
export function toNumber(value: number | string | null | undefined): number | undefined {
  if (value === null || value === undefined || value === '') return undefined;
  const n = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(n) ? n : undefined;
}

const money = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 2 });

export function formatPrice(value: number | string | null | undefined): string {
  const n = toNumber(value);
  if (n === undefined) return '';
  return money.format(n).replace(/\.00$/, '');
}

/** The site's host name, for showing under an item ("rei.com"). */
export function linkHost(link: string | null | undefined): string | undefined {
  if (!link) return undefined;
  try {
    return new URL(link).hostname.replace(/^www\./, '');
  } catch {
    return undefined;
  }
}
