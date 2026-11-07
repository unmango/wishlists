import type { MyPurchase } from '../api';
import { Card, ErrorMessage, Loading, PageHeader, Screen } from '../components/ui';
import { formatPrice, linkHost, toNumber } from '../format';
import { href, useMyPurchases } from '../hooks';

function groupByOwner(purchases: MyPurchase[]) {
  const groups = new Map<string, { ownerName: string; wishlistId: string; items: MyPurchase[] }>();
  for (const p of purchases) {
    const group = groups.get(p.ownerId) ?? { ownerName: p.ownerName, wishlistId: p.wishlistId, items: [] };
    group.items.push(p);
    groups.set(p.ownerId, group);
  }
  return [...groups.values()];
}

export function Buying() {
  const { data, error, isLoading } = useMyPurchases();
  const total = data?.reduce((sum, p) => sum + (toNumber(p.price) ?? 0), 0) ?? 0;
  const count = data?.length ?? 0;
  const eyebrow = data
    ? `${count} ${count === 1 ? 'gift' : 'gifts'}${total > 0 ? ` · ${formatPrice(total)} so far` : ''}`
    : undefined;

  return (
    <Screen>
      <PageHeader eyebrow={eyebrow} title='What I’m buying' />

      {isLoading && <Loading />}
      {error && <ErrorMessage error={error} />}
      {data && count === 0 && (
        <p className='px-5 text-muted'>Nothing yet. Tap “I’ll get this” on someone’s list and it shows up here.</p>
      )}

      <div className='flex flex-col gap-4.5 px-4'>
        {data
          && groupByOwner(data).map(group => (
            <section key={group.wishlistId} className='flex flex-col gap-2'>
              <h2 className='mx-1 my-0 text-[13px] font-bold tracking-wide text-muted uppercase'>
                For {group.ownerName.split(' ')[0]}
              </h2>
              <Card>
                <ul className='divide-y divide-line'>
                  {group.items.map(item => (
                    <li key={item.itemId}>
                      <a
                        href={href({ name: 'list', id: item.wishlistId })}
                        className='flex min-h-14 items-center gap-2.5 px-4 py-3 text-ink'
                      >
                        <div className='flex min-w-0 flex-1 flex-col gap-0.5'>
                          <span
                            className={`text-base font-medium break-words ${item.removedByOwner ? 'line-through' : ''}`}
                          >
                            {item.name}
                          </span>
                          <span className={`text-[13px] ${item.removedByOwner ? 'text-accent' : 'text-muted'}`}>
                            {item.removedByOwner
                              ? `${group.ownerName.split(' ')[0]} removed this from their list`
                              : (linkHost(item.link) ?? 'No link')}
                          </span>
                        </div>
                        <span className='text-[15px] font-bold'>{formatPrice(item.price)}</span>
                      </a>
                    </li>
                  ))}
                </ul>
              </Card>
            </section>
          ))}
      </div>
    </Screen>
  );
}
