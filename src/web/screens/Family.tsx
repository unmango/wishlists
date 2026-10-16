import { faChevronRight } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { type Me, type Member, toNumber } from '../api';
import { Avatar, Card, ErrorMessage, Loading, Screen } from '../components/ui';
import { href, signOut, useMembers } from '../hooks';

function summary(member: Member): string {
  const items = toNumber(member.itemCount) ?? 0;
  const reallyWant = toNumber(member.reallyWantCount) ?? 0;
  if (items === 0) return 'Nothing added yet';
  const base = `${items} ${items === 1 ? 'item' : 'items'}`;
  return reallyWant > 0 ? `${base} · ${reallyWant} really want` : base;
}

export function Family({ me }: { me: Me }) {
  const { data, error, isLoading } = useMembers();
  const others = data?.filter(m => m.id !== me.id) ?? [];
  const firstName = me.displayName.split(' ')[0];

  return (
    <Screen>
      <header className='mb-4 flex items-end gap-2 bg-hero px-5 pt-14 pb-6 text-white'>
        <div className='flex flex-1 flex-col gap-1'>
          <div className='text-sm text-hero-muted'>Hi, {firstName}</div>
          <h1 className='m-0 font-display text-[32px] leading-tight font-extrabold tracking-tight'>Family</h1>
        </div>
        <button
          type='button'
          onClick={() => void signOut()}
          className='h-11 px-1 text-sm text-hero-muted underline-offset-4 hover:underline'
        >
          Sign out
        </button>
      </header>

      {isLoading && <Loading />}
      {error && <ErrorMessage error={error} />}
      {data && others.length === 0 && (
        <p className='px-5 text-muted'>Nobody else has signed in yet. Their lists show up here once they do.</p>
      )}

      <ul className='flex flex-col gap-2.5 px-4'>
        {others.map(member => (
          <li key={member.id}>
            <Card>
              <a
                href={href({ name: 'list', id: member.wishlistId })}
                className='flex min-h-11 items-center gap-3.5 px-4 py-3.5 text-ink'
              >
                <Avatar id={member.id} name={member.displayName} url={member.avatarUrl} />
                <div className='flex min-w-0 flex-1 flex-col gap-0.5'>
                  <div className='truncate text-[17px] font-medium'>{member.displayName}</div>
                  <div className='text-[13px] text-muted'>{summary(member)}</div>
                </div>
                <FontAwesomeIcon icon={faChevronRight} className='text-faint' aria-hidden='true' />
              </a>
            </Card>
          </li>
        ))}
      </ul>
    </Screen>
  );
}
