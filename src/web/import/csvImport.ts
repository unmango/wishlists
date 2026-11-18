import type { ItemRequest } from '../api';
import { cleanPrice, isPlainPrice, isWebLink } from '../components/itemForm';

/** One item read from a CSV file, before the owner reviews it. */
export type ImportRow = {
  /** Empty when the file had no name for this row: the owner types one in the preview. */
  name: string;
  link: string | null;
  price: number | null;
  notes: string | null;
  reallyWant: boolean;
  /** Values that couldn't be understood and were dropped, such as "price not understood". */
  warnings: string[];
};

// The same limits as the item form and the API.
const maxName = 200;
const maxNotes = 2000;
const maxLink = 2000;

type Field = 'name' | 'link' | 'price' | 'notes' | 'reallyWant';

/** Header names for each field, matched ignoring case. Earlier aliases win when a file has more than one. */
const aliases: Record<Field, string[]> = {
  name: ['name', 'item', 'title', 'description'],
  link: ['link', 'product link', 'url'],
  price: ['price'],
  notes: ['notes', 'comments'],
  reallyWant: ['really want'],
};

/** Splits CSV text into rows of cells: comma-separated, with "quoted" cells that may hold commas, quotes and newlines. */
export function parseCsv(text: string): string[][] {
  const rows: string[][] = [];
  let row: string[] = [];
  let cell = '';
  let quoted = false;

  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (quoted) {
      if (c === '"' && text[i + 1] === '"') {
        cell += '"';
        i++;
      } else if (c === '"') quoted = false;
      else cell += c;
    } else if (c === '"') quoted = true;
    else if (c === ',') {
      row.push(cell);
      cell = '';
    } else if (c === '\n' || c === '\r') {
      if (c === '\r' && text[i + 1] === '\n') i++;
      row.push(cell);
      rows.push(row);
      row = [];
      cell = '';
    } else cell += c;
  }

  if (cell !== '' || row.length > 0) {
    row.push(cell);
    rows.push(row);
  }
  return rows;
}

/** Which column holds each field, from the header row. */
function columnsFor(header: string[]): Partial<Record<Field, number>> {
  const names = header.map(h => h.trim().toLowerCase());
  const columns: Partial<Record<Field, number>> = {};
  for (const [field, options] of Object.entries(aliases) as [Field, string[]][]) {
    const index = options.map(o => names.indexOf(o)).find(i => i >= 0);
    if (index !== undefined) columns[field] = index;
  }
  return columns;
}

function readRow(cells: string[], columns: Partial<Record<Field, number>>): ImportRow {
  const cell = (field: Field) => (columns[field] === undefined ? '' : (cells[columns[field]] ?? '').trim());
  const warnings: string[] = [];

  const rawLink = cell('link');
  const linkOk = rawLink === '' || (rawLink.length <= maxLink && isWebLink(rawLink));
  if (!linkOk) warnings.push('link not understood');

  const rawPrice = cleanPrice(cell('price'));
  const priceOk = rawPrice === '' || isPlainPrice(rawPrice);
  if (!priceOk) warnings.push('price not understood');

  const name = cell('name');
  if (name.length > maxName) warnings.push('name shortened');
  const notes = cell('notes');
  if (notes.length > maxNotes) warnings.push('notes shortened');

  return {
    name: name.slice(0, maxName).trim(),
    link: linkOk && rawLink ? rawLink : null,
    price: priceOk && rawPrice ? Number(rawPrice) : null,
    notes: notes.slice(0, maxNotes).trim() || null,
    reallyWant: ['yes', 'true', 'x', '1'].includes(cell('reallyWant').toLowerCase()),
    warnings,
  };
}

/** The most items one file may import. The API has the same limit. */
export const maxImportItems = 200;

/** Reads a CSV file's items for the import preview, or says why the file can't be imported. */
export function readImport(text: string): { rows: ImportRow[] } | { error: string } {
  // Excel starts UTF-8 files with a byte order mark, which would otherwise stick to the first header.
  const [header = [], ...body] = parseCsv(text.replace(/^\uFEFF/, ''));
  const columns = columnsFor(header);
  const filled = body.filter(cells => cells.some(c => c.trim() !== ''));

  if (filled.length === 0) return { error: 'That file has no items in it.' };
  if (Object.keys(columns).length === 0) {
    return {
      error: 'Couldn’t find a name, link or price column. The first row needs headers like Name, Link and Price.',
    };
  }
  if (filled.length > maxImportItems) {
    return {
      error:
        `That file has ${filled.length} items. Import up to ${maxImportItems} at a time by splitting it into smaller files.`,
    };
  }

  return { rows: filled.map(cells => readRow(cells, columns)) };
}

/** What happens to a row: added, skipped as already on the wishlist, or waiting for the owner to name it. */
export type ImportStatus = 'add' | 'duplicate' | 'needs-name';

export type PlannedRow = ImportRow & { status: ImportStatus };

/** Two items are the same when they share a link, or a name when neither has a link. The API matches the same way. */
function duplicateKey(item: { name: string; link: string | null }): string | undefined {
  const link = item.link?.trim();
  if (link) return `link:${link}`;
  const name = item.name.trim();
  return name ? `name:${name.toUpperCase()}` : undefined;
}

/** Decides each row's status against what's already on the wishlist. Earlier rows in the file win over later ones. */
export function planImport(rows: ImportRow[], existing: { name: string; link?: string | null }[]): PlannedRow[] {
  const seen = new Set(existing.map(x => duplicateKey({ name: x.name, link: x.link ?? null })));
  return rows.map(row => {
    const key = duplicateKey(row);
    if (key && seen.has(key)) return { ...row, status: 'duplicate' };
    if (!row.name.trim()) return { ...row, status: 'needs-name' };
    seen.add(key);
    return { ...row, status: 'add' };
  });
}

/** The rows to send when the owner taps "Add N items". */
export function importRequest(plan: PlannedRow[]): ItemRequest[] {
  return plan
    .filter(row => row.status === 'add')
    .map(({ name, link, price, notes, reallyWant }) => ({ name: name.trim(), link, price, notes, reallyWant }));
}

/** A file the owner picked: its items, or why it can't be imported. */
export type PickedFile = { name: string } & ({ rows: ImportRow[] } | { error: string });

/** Far bigger than 200 rows of any real wishlist, so a wrong file is refused before it's read. */
const maxFileBytes = 1_000_000;

/** Reads the file the owner picked from the phone's file picker. */
export async function readCsvFile(file: File): Promise<PickedFile> {
  const { name } = file;
  if (!/\.csv$/i.test(name)) {
    return { name, error: 'Pick a .csv file. Save spreadsheets from Excel or Numbers as CSV first.' };
  }
  if (file.size > maxFileBytes) return { name, error: 'That file is too big to be a wishlist.' };
  try {
    return { name, ...readImport(await file.text()) };
  } catch {
    return { name, error: 'Couldn’t read that file. Try picking it again.' };
  }
}
