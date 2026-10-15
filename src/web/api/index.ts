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
