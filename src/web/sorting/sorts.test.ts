import { describe, expect, test } from 'bun:test';
import { type Sort, sortItems, sortLabel, toSort } from './sorts';

type Item = { name: string; reallyWant: boolean; price: number | string | null };
const item = (name: string, reallyWant = false, price: number | string | null = null): Item => ({
  name,
  reallyWant,
  price,
});
const names = (items: Item[], sort: Sort) => sortItems(items, sort).map(x => x.name);

describe('sortItems', () => {
  const items = [
    item('A', false, 20),
    item('B', true, null),
    item('C', false, '5.5'),
    item('D', true, 20),
    item('E', false, null),
  ];

  test('order keeps the owner’s order', () => {
    expect(names(items, 'order')).toEqual(['A', 'B', 'C', 'D', 'E']);
  });

  test('really want first groups really-want items on top, keeping the order inside each group', () => {
    expect(names(items, 'reallyWant')).toEqual(['B', 'D', 'A', 'C', 'E']);
  });

  test('price high to low puts no-price items last, and ties keep the order', () => {
    expect(names(items, 'priceHigh')).toEqual(['A', 'D', 'C', 'B', 'E']);
  });

  test('price low to high puts no-price items last too, and ties keep the order', () => {
    expect(names(items, 'priceLow')).toEqual(['C', 'A', 'D', 'B', 'E']);
  });

  test('a free item has a price', () => {
    expect(names([item('None'), item('Free', false, 0)], 'priceLow')).toEqual(['Free', 'None']);
    expect(names([item('None'), item('Free', false, 0)], 'priceHigh')).toEqual(['Free', 'None']);
  });

  test('never changes the list it was given', () => {
    const copy = [...items];
    sortItems(items, 'priceLow');
    expect(items).toEqual(copy);
  });
});

describe('sortLabel', () => {
  test('names the order after the owner, or "My order" on your own list', () => {
    expect(sortLabel('order', 'Bob')).toBe('Bob’s order');
    expect(sortLabel('order', undefined)).toBe('My order');
    expect(sortLabel('priceHigh', 'Bob')).toBe('Price high to low');
  });
});

describe('toSort', () => {
  test('uses the order for anything unknown', () => {
    expect(toSort('priceLow')).toBe('priceLow');
    expect(toSort('price')).toBe('order');
    expect(toSort(null)).toBe('order');
  });
});
