import type { OwnItem } from '../api';
import { formatPrice, linkHost } from '../format';
import { ReallyWantStar, Thumbnail } from './ui';

function itemMeta(item: OwnItem): string {
  const parts = [linkHost(item.link) ?? 'No link'];
  if (item.notes) parts.push(item.notes.split('\n')[0]!);
  return parts.join(' · ');
}

/** One of my items in a row: thumbnail, name, link host or first line of notes, and price. */
export function OwnItemSummary({ item }: { item: OwnItem }) {
  return (
    <>
      <Thumbnail src={item.thumbnail} />
      <div className='flex min-w-0 flex-1 flex-col gap-0.5'>
        <div className='flex items-center gap-1.5'>
          {item.reallyWant && <ReallyWantStar label='Really want' />}
          <span className='text-base font-medium break-words'>{item.name}</span>
        </div>
        <div className='truncate text-[13px] text-muted'>{itemMeta(item)}</div>
      </div>
      <div className='text-[15px] font-bold'>{formatPrice(item.price)}</div>
    </>
  );
}
