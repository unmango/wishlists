import type { Middleware } from 'openapi-fetch';
import * as token from './token';

export const middleware: Middleware = {
  async onRequest({ request }): Promise<Request> {
    const tok = token.get();
    if (tok && pathPrefix(request).startsWith('/api')) {
      console.debug('setting auth header');
      request.headers.set('Authorization', `Bearer ${tok}`);
    }

    return request;
  },
};

function pathPrefix(req: Request): string {
	return new URL(req.url).pathname;
}
