import { useCallback, useState } from 'react';
import { type AccessTokenResponse, defaultClient, type ProblemDetails } from './api';
import { Login } from './components';

function App() {
  const [loginError, setLoginError] = useState<ProblemDetails>();
  const [registerError, setRegisterError] = useState<ProblemDetails>();
  const [tokenResponse, setTokenResponse] = useState<AccessTokenResponse>();

  const clearErrors = useCallback(() => {
		setLoginError(undefined);
		setRegisterError(undefined);
	}, []);

  const token = tokenResponse?.accessToken;
  const error = loginError || registerError;

  return (
    <div className='h-svh w-svw flex flex-col gap-4 p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <div className='w-full flex justify-center'>
        <div className='w-1/3 text-center rounded-full bg-black/25 p-2'>
          <h1 className='text-xl text-white'>The Wishlists App</h1>
        </div>
      </div>

      <div className='w-full h-full flex flex-col items-center justify-center'>
        {error && (
          <div className='bg-red-600/75 text-white p-4 rounded-lg'>
            <h2 className='font-bold'>Error</h2>
            <pre>{JSON.stringify(loginError, null, 2)}</pre>
            <div className='p-2'>
              <button className='bg-black/25 w-full rounded-full p-2 hover:bg-white/15' onClick={clearErrors}>
                Try Again
              </button>
            </div>
          </div>
        )}
        {!token && !error && (
          <Login
            client={defaultClient}
            onLoginFailed={setLoginError}
            onLoginSuccess={setTokenResponse}
            onRegisterFailed={setRegisterError}
          />
        )}
      </div>
    </div>
  );
}

export default App;
