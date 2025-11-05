import type { AccessTokenResponse } from ".";

const key = 'UNMANGO_token';

export function get(): string | null {
	return localStorage.getItem(key);
}

export function set(token: string): void {
	localStorage.setItem(key, token);
}

export function setFrom(res: AccessTokenResponse): void {
	set(res.accessToken);
}
