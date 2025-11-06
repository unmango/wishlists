import { type JSX, useEffect, useState } from 'react';
import { type Client, type ProblemDetails, type User } from '../api';

export interface Props {
  client: Client;
}

function Editor({ client }: Props): JSX.Element {
  const [me, setMe] = useState<User>();
  const [error, setError] = useState<ProblemDetails>();

  useEffect(() => {
    client.GET('/api/me').then(({ data, error }) => {
      if (data) setMe(data);
      if (error) setError(error);
    });
  }, [client]);

  if (error) {
    return (
      <div className='bg-red-500 rounded-full w-4/6'>
        <pre>{JSON.stringify(error, null, 2)}</pre>
      </div>
    );
  }

  return (
    <div className='w-full h-full flex flex-col items-center gap-4'>
      <div className='w-full text-center rounded-full bg-black/25 p-2'>
        <span className='text-white'>Hello, {me?.userName}!</span>
      </div>
      <div className='w-full h-full flex gap-4'>
        <div className='h-full w-1/6 bg-black/25 rounded-xl p-2'>
        </div>
        <div className='h-full w-5/6 bg-black/25 rounded-xl p-2'>
        </div>
      </div>
    </div>
  );
}

export default Editor;
