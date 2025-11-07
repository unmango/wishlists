import { type JSX, useCallback, useState } from 'react';
import type { AccessTokenResponse, Client, ProblemDetails } from '../../api';
import TextBox from '../TextBox';

export interface Props {
  client: Client;
  onLoginSuccess?(data: AccessTokenResponse): void;
  onLoginFailed?(error: ProblemDetails): void;
  onRegisterSuccess?(): void;
  onRegisterFailed?(error: ProblemDetails): void;
}

export function Login(
  { client, onLoginSuccess, onLoginFailed, onRegisterFailed, onRegisterSuccess }: Props,
): JSX.Element {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [signin, setSignin] = useState(true);
  const [loading, setLoading] = useState(false);

  const login = useCallback(async () => {
    setLoading(true);
    const { data, error } = await client.POST('/auth/login', {
      body: { email, password },
    });

    setLoading(false);
    if (data && onLoginSuccess) {
      onLoginSuccess(data);
    }
    if (error && onLoginFailed) {
      onLoginFailed(error);
    }
  }, [client, email, onLoginFailed, onLoginSuccess, password]);

  const register = useCallback(async () => {
    setLoading(true);
    const { error } = await client.POST('/auth/register', {
      body: { email, password },
    });

    setLoading(false);
    if (onRegisterSuccess) onRegisterSuccess();
    if (error && onRegisterFailed) onRegisterFailed(error);
  }, [client, email, onRegisterFailed, onRegisterSuccess, password]);

  if (loading) {
    return (
      <div className='w-1/3 p-8 flex flex-col gap-4 text-center rounded-xl bg-black/25'>
        Loading...
      </div>
    );
  }

  return (
    <div className='w-1/3 p-8 flex flex-col gap-4 text-center rounded-xl bg-black/25'>
      <h2 className='text-white text-2xl'>
        {signin ? 'Ready to get planning?' : "Let's get planning!"}
      </h2>
      <div className='flex flex-col gap-4'>
        <TextBox
          name='email'
          type='email'
          placeholder='Email'
          value={email}
          onChange={setEmail}
        />
        <TextBox
          name='password'
          type='password'
          placeholder='Password'
          value={password}
          onChange={setPassword}
        />
      </div>
      <button
        className='w-full px-4 py-2 bg-black/25 text-white rounded-full hover:bg-white/15 cursor-pointer'
        type='button'
        onClick={signin ? login : register}
      >
        {signin ? 'Sign In' : 'Register'}
      </button>
      <button
        className='w-full px-4 py-2 bg-black/25 text-white rounded-full hover:bg-white/15 cursor-pointer'
        type='button'
        onClick={() => setSignin(x => !x)}
      >
        {signin ? 'Create an Account' : 'Have an Account? Sign In'}
      </button>
    </div>
  );
}
