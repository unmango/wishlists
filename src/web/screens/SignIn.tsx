import { faGift } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';

const messages: Record<string, string> = {
  denied: 'That Google account isn’t on the family list. Ask Erik to add it.',
  failed: 'Signing in with Google didn’t finish. Try again.',
};

function GoogleLogo() {
  return (
    <svg aria-hidden='true' width='20' height='20' viewBox='0 0 24 24'>
      <path
        fill='#4285F4'
        d='M22.5 12.3c0-.8-.1-1.5-.2-2.2H12v4.2h5.9a5 5 0 0 1-2.2 3.3v2.7h3.5c2.1-1.9 3.3-4.7 3.3-8z'
      />
      <path
        fill='#34A853'
        d='M12 23c3 0 5.4-1 7.2-2.7l-3.5-2.7c-1 .7-2.2 1-3.7 1-2.9 0-5.3-1.9-6.2-4.5H2.2v2.8A11 11 0 0 0 12 23z'
      />
      <path fill='#FBBC05' d='M5.8 14.1a6.6 6.6 0 0 1 0-4.2V7.1H2.2a11 11 0 0 0 0 9.8z' />
      <path
        fill='#EA4335'
        d='M12 5.4c1.6 0 3.1.6 4.2 1.7l3.1-3.1A11 11 0 0 0 2.2 7.1l3.6 2.8C6.7 7.3 9.1 5.4 12 5.4z'
      />
    </svg>
  );
}

export function SignIn() {
  const status = new URLSearchParams(window.location.search).get('signin') ?? '';
  const message = messages[status];
  const returnUrl = encodeURIComponent(`/${window.location.hash}`);

  return (
    <main className='flex min-h-svh flex-col bg-[#2b2d6e] px-7 pb-14 text-white'>
      <div className='mx-auto flex w-full max-w-md flex-1 flex-col justify-end gap-7'>
        <div className='flex flex-1 items-center justify-center'>
          <FontAwesomeIcon icon={faGift} className='text-[112px] opacity-90' aria-hidden='true' />
        </div>
        <div className='flex flex-col gap-2.5'>
          <h1 className='m-0 font-display text-[40px] font-extrabold tracking-tight'>Wishlists</h1>
          <p className='m-0 text-[17px] leading-relaxed text-[#d6d7f0]'>
            Make your list, see the family’s, and quietly claim gifts. Nobody sees what’s been bought for them.
          </p>
        </div>
        {message && (
          <p role='alert' className='m-0 rounded-xl bg-white px-3.5 py-3 text-sm leading-snug text-[#8a1f1f]'>
            {message}
          </p>
        )}
        <a
          href={`/auth/login?returnUrl=${returnUrl}`}
          className='flex h-13 items-center justify-center gap-2.5 rounded-xl bg-white text-[17px] font-medium text-[#1a1b2e]'
        >
          <GoogleLogo />
          Continue with Google
        </a>
      </div>
    </main>
  );
}
