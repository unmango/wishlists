import { describe, expect, test } from 'bun:test';
import { type ImportRow, planImport, readImport } from './csvImport';

function rowsOf(csv: string): ImportRow[] {
  const result = readImport(csv);
  if ('error' in result) throw new Error(result.error);
  return result.rows;
}

describe('readImport', () => {
  test('reads items from alias headers, ignoring case and extra columns', () => {
    const csv = [
      'Description,Product Link,PRICE,Comments,Really Want,Store aisle',
      'Wool socks,https://example.com/socks,12.50,Size L,yes,7',
    ].join('\n');

    expect(readImport(csv)).toEqual({
      rows: [{
        name: 'Wool socks',
        link: 'https://example.com/socks',
        price: 12.5,
        notes: 'Size L',
        reallyWant: true,
        warnings: [],
      }],
    });
  });

  test('reads prices like $12.99 and 1,200, and really want as yes, true, x or 1', () => {
    const csv = [
      'name,price,really want',
      'A,$12.99,Yes',
      'B,"1,200",TRUE',
      'C,7,x',
      'D,,1',
      'E,3,no',
      'F,4,',
    ].join('\n');

    const rows = rowsOf(csv);

    expect(rows.map(r => r.price)).toEqual([12.99, 1200, 7, null, 3, 4]);
    expect(rows.map(r => r.reallyWant)).toEqual([true, true, true, true, false, false]);
    expect(rows.flatMap(r => r.warnings)).toEqual([]);
  });

  test('keeps a row whose price or link is not understood, without that field and with a warning', () => {
    const csv = [
      'name,link,price',
      'Socks,not a link,about ten',
      'Hat,javascript:alert(1),-5',
      'Scarf,https://example.com/scarf,12.999',
    ].join('\n');

    const rows = rowsOf(csv);

    expect(rows.map(r => [r.name, r.link, r.price])).toEqual([
      ['Socks', null, null],
      ['Hat', null, null],
      ['Scarf', 'https://example.com/scarf', null],
    ]);
    expect(rows.map(r => r.warnings)).toEqual([
      ['link not understood', 'price not understood'],
      ['link not understood', 'price not understood'],
      ['price not understood'],
    ]);
  });

  test('reads files saved by spreadsheets: quoted cells, CRLF line ends, a byte order mark and blank lines', () => {
    const csv = '\uFEFFName,Notes\r\n'
      + '"Socks, wool","Size L\r\nnot ""XL"""\r\n'
      + ',\r\n'
      + '\r\n'
      + 'Hat,\r\n';

    const rows = rowsOf(csv);

    expect(rows.map(r => [r.name, r.notes])).toEqual([
      ['Socks, wool', 'Size L\r\nnot "XL"'],
      ['Hat', null],
    ]);
  });

  test('keeps a quote inside an unquoted cell, like an inch mark', () => {
    const csv = 'name,price\nTV 55" wide,300\nSocks,5';

    expect(rowsOf(csv).map(r => [r.name, r.price])).toEqual([['TV 55" wide', 300], ['Socks', 5]]);
  });

  test('drops a price too big to be real, with a warning, so it cannot fail the whole import', () => {
    const [row] = rowsOf(`name,price\nSocks,${'9'.repeat(30)}`);

    expect(row!.price).toBeNull();
    expect(row!.warnings).toEqual(['price not understood']);
  });

  test('skips blank lines before the header, and rows that only fill columns it ignores', () => {
    const csv = '\n,\nName,Aisle\nSocks,3\n,7\n';

    expect(rowsOf(csv).map(r => r.name)).toEqual(['Socks']);
  });

  test('refuses a file with no columns it knows', () => {
    expect(readImport('Thing,Cost\nSocks,12')).toEqual({
      error: 'Couldn’t find a name, link or price column. The first row needs headers like Name, Link and Price.',
    });
  });

  test('refuses a file with no items', () => {
    expect(readImport('')).toEqual({ error: 'That file has no items in it.' });
    expect(readImport('Name,Link\n,\n')).toEqual({ error: 'That file has no items in it.' });
  });

  test('takes up to 200 items from one file', () => {
    const csv = (count: number) => ['Name', ...Array.from({ length: count }, (_, i) => `Item ${i}`)].join('\n');

    expect(rowsOf(csv(200))).toHaveLength(200);
    expect(readImport(csv(201))).toEqual({
      error: 'That file has 201 items. Import up to 200 at a time by splitting it into smaller files.',
    });
  });

  test('shortens names and notes that are too long, with a warning', () => {
    const csv = `Name,Notes\n${'n'.repeat(250)},${'x'.repeat(2500)}`;

    const [row] = rowsOf(csv);

    expect(row!.name).toHaveLength(200);
    expect(row!.notes).toHaveLength(2000);
    expect(row!.warnings).toEqual(['name shortened', 'notes shortened']);
  });
});

describe('planImport', () => {
  const row = (name: string, link: string | null = null): ImportRow => ({
    name,
    link,
    price: null,
    notes: null,
    reallyWant: false,
    warnings: [],
  });

  test('adds new rows, skips ones already on the wishlist or earlier in the file, and asks for missing names', () => {
    const existing = [
      { name: 'Socks', link: 'https://example.com/socks' },
      { name: 'Hat', link: null },
    ];
    const rows = [
      row('Other name, same link', 'https://example.com/socks'),
      row('hat'),
      row('Socks'), // Same name, but the existing Socks has a link.
      row('Hat', 'https://example.com/hat'), // Same name, but this one has a link.
      row('Scarf'),
      row(' scarf '),
      row(''),
      row('', 'https://example.com/gloves'),
    ];

    expect(planImport(rows, existing).map(r => r.status)).toEqual([
      'duplicate',
      'duplicate',
      'add',
      'add',
      'add',
      'duplicate',
      'needs-name',
      'needs-name',
    ]);
  });

  test('a row with a link already on the wishlist is a duplicate even before it has a name', () => {
    const plan = planImport([row('', 'https://example.com/socks')], [{
      name: 'Socks',
      link: 'https://example.com/socks',
    }]);

    expect(plan.map(r => r.status)).toEqual(['duplicate']);
  });
});
