import { useCallback, useEffect, useState } from 'react';
import { type AccessTokenResponse, client, type User } from './api';
import Editor from './components/Editor';
import Register from './components/Register';
import SignIn from './components/SignIn';

function App() {
  const [me] = useState<User | null>(null);
  const [register, setRegister] = useState(false);

  const loginSuccess = useCallback((res: AccessTokenResponse) => {
    console.log('Got here: %s', res.accessToken);
  }, []);

  useEffect(() => {
    // api.me().then(setMe).catch(console.error);
  }, []);

  return (
    <div className='h-svh w-svw flex flex-col gap-4 p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <div className='w-full flex justify-center'>
        <div className='w-1/3 text-center rounded-full bg-black/25 p-2'>
          <h1 className='text-xl text-white'>The Wishlists App</h1>
        </div>
      </div>
      {me && <Editor me={me} />}
      {!me && register && <Register client={client} onSignIn={() => setRegister(false)} />}
      {!me && !register && (
        <SignIn
          client={client}
          onLoginSuccess={loginSuccess}
          onRegister={() => setRegister(true)}
        />
      )}
    </div>
  );
}

export default App;
