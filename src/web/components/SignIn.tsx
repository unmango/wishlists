import { type JSX, useCallback, useReducer } from 'react';
import type { AccessTokenResponse, Client, ProblemDetails } from '../api';
import TextBox from './TextBox';

export interface Props {
  client: Client;
  onLoginSuccess(response: AccessTokenResponse): void;
  onRegister(): void;
}

interface EmailChanged {
  type: 'emailChanged';
  payload: string;
}

interface PasswordChanged {
  type: 'passwordChanged';
  payload: string;
}

interface LoginSuccess {
  type: 'loginSuccess';
  payload: AccessTokenResponse;
}

interface LoginFailed {
  type: 'loginFailed';
  payload: ProblemDetails;
}

type Action = EmailChanged | LoginFailed | LoginSuccess | PasswordChanged;

interface State {
  email: string;
  password: string;
  errors: string[];
}

const initialState: State = {
  email: '',
  password: '',
  errors: [],
};

function reducer(state: State, { type, payload }: Action): State {
  switch (type) {
    case 'emailChanged':
      return { ...state, email: payload };
    case 'passwordChanged':
      return { ...state, password: payload };
    case 'loginFailed':
      return { ...state, errors: Object.values(payload.errors ?? {}).flat() };
    default:
      return state;
  }
}

function SignIn({ client, onLoginSuccess, onRegister }: Props): JSX.Element {
  const [{ email, password }, dispatch] = useReducer(reducer, initialState);

  const login = useCallback(() => {
    client.POST('/auth/login', {
      body: { email, password },
	  credentials: 'include',
    })
      .then(({ data, error }) => {
		if (error) console.error(error);
		else if (data) onLoginSuccess(data);
	  })
      .catch(console.error);
  }, [client, email, onLoginSuccess, password]);

  return (
    <div className='w-full h-full flex flex-col items-center justify-center'>
      <div className='w-1/3 p-4 flex flex-col gap-4 text-center rounded-xl bg-black/25'>
        <h2 className='text-white text-2xl'>Ready to get planning?</h2>
        <div className='flex flex-col gap-4'>
          <TextBox
            name='email'
            type='email'
            placeholder='Email'
            value={email}
            onChange={(value) => dispatch({ type: 'emailChanged', payload: value })}
          />
          <TextBox
            name='password'
            type='password'
            placeholder='Password'
            value={password}
            onChange={(value) => dispatch({ type: 'passwordChanged', payload: value })}
          />
        </div>
        <button
          className='w-full px-4 py-2 bg-black/25 text-white rounded-full hover:bg-white/15 cursor-pointer'
          type='button'
          onClick={login}
        >
          Sign In
        </button>
        <button
          className='w-full px-4 py-2 bg-black/25 text-white rounded-full hover:bg-white/15 cursor-pointer'
          type='button'
          onClick={onRegister}
        >
          Register
        </button>
      </div>
    </div>
  );
}

export default SignIn;
