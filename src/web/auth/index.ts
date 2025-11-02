export interface RegisterRequest {
	email: string;
	password: string;
}

export async function register(req: RegisterRequest): Promise<void> {
	const res = await fetch('/auth/register', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json',
		},
		body: JSON.stringify(req),
	});

	if (!res.ok) {
		return Promise.reject(res.body);
	}
}
