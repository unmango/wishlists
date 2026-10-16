import { faCheck, faChevronLeft } from '@fortawesome/free-solid-svg-icons';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { useState } from 'react';
import { formatPrice, type GiftItem, linkHost, type Me } from '../api';
import { Card, ErrorMessage, Loading, ReallyWantStar, Screen } from '../components/ui';
import { href, markBought, unmarkBought, useWishlist } from '../hooks';

function ItemRow(
  { item, wishlistId, ownerName, me }: { item: GiftItem; wishlistId: string; ownerName: string; me: Me },
) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>();
  const mine = item.boughtBy === me.id;
  const host = linkHost(item.link);

  async function run(action: () => Promise<void>) {
    setBusy(true);
    setError(undefined);
    try {
      await action();
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  }

  return (
    <li>
      <Card className={`flex flex-col gap-2.5 px-4 py-3.5 ${item.removedByOwner ? 'opacity-70' : ''}`}>
        <div className='flex items-start gap-2.5'>
          <div className='flex min-w-0 flex-1 flex-col gap-0.5'>
            <div className='flex items-center gap-1.5'>
              {item.reallyWant && <ReallyWantStar label={`${ownerName} really wants this`} />}
              <span className={`text-base font-medium break-words ${item.removedByOwner ? 'line-through' : ''}`}>
                {item.name}
              </span>
            </div>
            {item.removedByOwner
              ? <div className='text-[13px] text-muted'>Removed by {ownerName}</div>
              : host
              ? (
                <a
                  href={item.link!}
                  target='_blank'
                  rel='noopener noreferrer'
                  className='self-start text-[13px] text-primary underline-offset-2 hover:underline'
                >
                  {host}
                </a>
              )
              : <div className='text-[13px] text-muted'>No link</div>}
            {item.notes && <p className='m-0 text-[13px] whitespace-pre-line text-soft'>{item.notes}</p>}
          </div>
          <div className='text-[15px] font-bold'>{formatPrice(item.price)}</div>
        </div>

        {!item.boughtBy && !item.removedByOwner && (
          <button
            type='button'
            disabled={busy}
            onClick={() => void run(() => markBought(wishlistId, item.id))}
            className='h-11 rounded-[10px] border-[1.5px] border-primary bg-card text-[15px] font-medium text-primary disabled:opacity-50'
          >
            I’ll get this
          </button>
        )}

        {mine && (
          <div className='flex items-center gap-2'>
            <div className='flex flex-1 items-center gap-1.5 text-sm font-medium text-primary'>
              <FontAwesomeIcon icon={faCheck} aria-hidden='true' />
              {item.removedByOwner ? `You got this (${ownerName} removed it)` : 'You’re getting this'}
            </div>
            <button
              type='button'
              disabled={busy}
              onClick={() => void run(() => unmarkBought(wishlistId, item.id))}
              className='h-11 rounded-[10px] bg-chip px-3.5 text-sm text-soft disabled:opacity-50'
            >
              Undo
            </button>
          </div>
        )}

        {item.boughtBy && !mine && (
          <div className='text-sm text-muted'>{item.boughtByName ?? 'Someone'} is getting this</div>
        )}

        {error !== undefined && (
          <p role='alert' className='m-0 text-sm text-danger'>
            {error instanceof Error ? error.message : 'That didn’t work. Try again.'}
          </p>
        )}
      </Card>
    </li>
  );
}

export function TheirList({ id, me }: { id: string; me: Me }) {
  const { data, error, isLoading } = useWishlist(id);
  const ownerName = data?.ownerName.split(' ')[0] ?? '';

  return (
    <Screen>
      <header className='flex items-center gap-1 px-3 pt-12 pb-3'>
        <a
          href={href({ name: 'family' })}
          aria-label='Back to Family'
          className='flex size-11 shrink-0 items-center justify-center text-ink'
        >
          <FontAwesomeIcon icon={faChevronLeft} className='text-xl' aria-hidden='true' />
        </a>
        <div className='flex min-w-0 flex-col'>
          <h1 className='m-0 font-display text-2xl font-extrabold tracking-tight break-words'>
            {data?.name ?? 'Wishlist'}
          </h1>
          {data && <div className='text-[13px] text-muted'>{ownerName} can’t see who’s getting what</div>}
        </div>
      </header>

      {isLoading && <Loading />}
      {error && <ErrorMessage error={error} />}
      {data && data.items.length === 0 && <p className='px-5 text-muted'>{ownerName} hasn’t added anything yet.</p>}

      <ul className='flex flex-col gap-2.5 px-4 pt-1'>
        {data?.items.map(item => (
          <ItemRow key={item.id} item={item} wishlistId={data.id} ownerName={ownerName} me={me} />
        ))}
      </ul>
    </Screen>
  );
}
