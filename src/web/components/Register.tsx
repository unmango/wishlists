import { type JSX, useCallback, useReducer } from 'react';
import { type Client, type ProblemDetails } from '../api';

export interface Props {
  client: Client;
}

interface State {
  email: string;
  password: string;
  emailErrors: string[];
  passwordErrors: string[];
}

interface EmailChanged {
  type: 'emailChanged';
  payload: string;
}

interface PasswordChanged {
  type: 'passwordChanged';
  payload: string;
}

interface ErrorReceived {
  type: 'errorReceived';
  payload?: ProblemDetails;
}

type Action = EmailChanged | PasswordChanged | ErrorReceived;

const initialState: State = {
  email: '',
  password: '',
  emailErrors: [],
  passwordErrors: [],
};

function emailErrors(details?: ProblemDetails): string[] {
  return details?.errors?.InvalidEmail ?? [];
}

function passwordErrors(details?: ProblemDetails): string[] {
  return Object.entries(details?.errors ?? {})
    .filter(([key]) => key.startsWith('Password'))
    .flatMap(([, msgs]) => msgs);
}

function reducer(state: State, { type, payload }: Action): State {
  switch (type) {
    case 'emailChanged':
      return { ...state, email: payload };
    case 'passwordChanged':
      return { ...state, password: payload };
    case 'errorReceived':
      return {
        ...state,
        emailErrors: emailErrors(payload),
        passwordErrors: passwordErrors(payload),
      };
    default:
      return state;
  }
}

function Register({ client }: Props): JSX.Element {
  const [{ email, password, emailErrors, passwordErrors }, dispatch] = useReducer(reducer, initialState);

  const register = useCallback(() => {
    client.POST('/auth/register', {
      body: { email, password },
    })
      .then(({ error }) => dispatch({ type: 'errorReceived', payload: error }))
      .catch(console.error);
  }, [client, email, password]);

  return (
    <div className='w-full h-full flex flex-col items-center justify-center'>
      <div className='w-1/3 p-4 flex flex-col gap-4 text-center rounded-xl bg-black/25'>
        <h2 className='text-white text-2xl'>Ready to get planning?</h2>
        <div className='flex flex-col gap-4'>
          <input
            className='w-full px-4 py-2 rounded-full bg-black/25 text-white placeholder-white/50 focus:outline-none focus:ring-2 focus:ring-white/50'
            name='email'
            type='email'
            placeholder='Email'
            value={email}
            onChange={(e) => dispatch({ type: 'emailChanged', payload: e.target.value })}
          />
          {emailErrors.length > 0 && (
            <div className='text-red-500 text-sm text-left'>
              {emailErrors.map((err, idx) => <div key={idx}>{err}</div>)}
            </div>
          )}
          <input
            className='w-full px-4 py-2 rounded-full bg-black/25 text-white placeholder-white/50 focus:outline-none focus:ring-2 focus:ring-white/50'
            name='password'
            type='password'
            placeholder='Password'
            value={password}
            onChange={(e) => dispatch({ type: 'passwordChanged', payload: e.target.value })}
          />
          {passwordErrors.length > 0 && (
            <div className='text-red-500 text-sm text-left'>
              {passwordErrors.map((err, idx) => <div key={idx}>{err}</div>)}
            </div>
          )}
        </div>
        <button
          className='w-full px-4 py-2 bg-black/25 text-white rounded-full hover:bg-white/15'
          type='button'
          onClick={register}
        >
          Register
        </button>
      </div>
    </div>
  );
}

export default Register;
