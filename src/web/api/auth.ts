import type { Middleware } from 'openapi-fetch';

export const middleware = (token: string): Middleware => ({
  async onRequest({ request }): Promise<Request> {
    if (pathPrefix(request).startsWith('/api')) {
      request.headers.set('Authorization', `Bearer ${token}`);
    }

    return request;
  },
});

function pathPrefix(req: Request): string {
  return new URL(req.url).pathname;
}
