import { useCallback, useEffect, useState } from 'react';
import { type AccessTokenResponse, client, token, type User } from './api';
import { Editor, Register, SignIn } from './components';

function App() {
  const [me, setMe] = useState<User | null>(null);
  const [register, setRegister] = useState(false);
  const [loading, setLoading] = useState(true);

  const loginSuccess = useCallback((res: AccessTokenResponse) => {
    console.debug('setting access token');
    token.set(res.accessToken);
    setLoading(false);
  }, []);

  useEffect(() => {
    client.GET('/api/me')
      .then(({ data, error }) => {
        if (data) setMe(data);
        else if (error) console.error(error);
      });
    // api.me().then(setMe).catch(console.error);
  }, []);

  return (
    <div className='h-svh w-svw flex flex-col gap-4 p-4 bg-linear-to-tr from-fuchsia-800 to-indigo-800'>
      <div className='w-full flex justify-center'>
        <div className='w-1/3 text-center rounded-full bg-black/25 p-2'>
          <h1 className='text-xl text-white'>The Wishlists App</h1>
        </div>
      </div>
      {loading && (
        <div className='bg-black/25 rounded-full'>
          <h2 className='text-white p-2'>Loading...</h2>
        </div>
      )}
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
