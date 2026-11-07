import { useEffect, useState } from 'react';

export type Route =
  | { name: 'family' }
  | { name: 'list'; id: string }
  | { name: 'mine' }
  | { name: 'buying' };

/** Hash routes keep the server simple: every screen is served by the same index.html. */
export function parseRoute(hash: string): Route {
  const [first, second] = hash.replace(/^#\/?/, '').split('/');
  if (first === 'list' && second) return { name: 'list', id: second };
  if (first === 'mine') return { name: 'mine' };
  if (first === 'buying') return { name: 'buying' };
  return { name: 'family' };
}

export function href(route: Route): string {
  switch (route.name) {
    case 'family':
      return '#/';
    case 'list':
      return `#/list/${route.id}`;
    case 'mine':
      return '#/mine';
    case 'buying':
      return '#/buying';
  }
}

export function useRoute(): Route {
  const [route, setRoute] = useState(() => parseRoute(window.location.hash));

  useEffect(() => {
    const onChange = () => {
      setRoute(parseRoute(window.location.hash));
      window.scrollTo(0, 0);
    };
    window.addEventListener('hashchange', onChange);
    return () => window.removeEventListener('hashchange', onChange);
  }, []);

  return route;
}
