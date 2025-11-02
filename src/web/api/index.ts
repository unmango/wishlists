export interface User {
	id: string;
	name: string;
}

export function me(): Promise<User> {
	return fetch('/api/me').then(res => res.json());
}
