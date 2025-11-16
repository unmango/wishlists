import { useAuth } from 'react-oidc-context';
import * as api from './api';
import { Landing } from './components';
import { ApiProvider } from './hooks';

function App() {
  const auth = useAuth();

  switch (auth.activeNavigator) {
    case 'signinSilent':
      return <div>Signing you in...</div>;
    case 'signoutRedirect':
      return <div>Signing you out...</div>;
  }

  if (auth.isLoading) {
    return <div>Loading...</div>;
  }

  if (auth.error) {
    return <div>Oops... {auth.error.name} caused {auth.error.message}</div>;
  }

  if (auth.isAuthenticated) {
    return (
      <div>
        Hello {auth.user?.profile.sub} <button onClick={() => void auth.removeUser()}>Log out</button>
      </div>
    );
  }

  return (
    <div className='h-svh w-svw flex flex-col gap-4 p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <div className='w-full flex justify-center'>
        <div className='w-2/3 text-center rounded-full bg-black/25 p-2'>
          <h1 className='text-xl text-white'>The Wishlists App</h1>
        </div>
      </div>

      <div className='w-full h-full flex flex-col items-center justify-center'>
        {
          /* {error && (
          <div className='bg-red-600/75 text-white p-4 rounded-lg'>
            <h2 className='font-bold'>Error</h2>
            <pre>{JSON.stringify(error, null, 2)}</pre>
            <div className='p-2'>
              <button className='bg-black/25 w-full rounded-full p-2 hover:bg-white/15' onClick={clearErrors}>
                Try Again
              </button>
            </div>
          </div>
        )} */
        }
        {
          /* {!token && !error && (
          <Login
            client={api.defaultClient}
            onLoginFailed={setLoginError}
            onLoginSuccess={handleLoginSuccess}
            onRegisterFailed={setRegisterError}
            onRegisterSuccess={handleRegisterSuccess}
          />
        )} */
        }
        {!auth.user && <button onClick={() => void auth.signinRedirect()}>Log in</button>}
        {auth.user && (
          <ApiProvider value={api.client(auth.user.access_token)}>
            <Landing />
          </ApiProvider>
        )}
      </div>
    </div>
  );
}

export default App;
