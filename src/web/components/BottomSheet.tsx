import { type ReactNode, useEffect, useRef } from 'react';

type Props = {
  open: boolean;
  onClose: () => void;
  /** Id of the element that names the sheet, usually its heading. */
  labelledBy: string;
  children: ReactNode;
};

/** A modal sheet that slides up from the bottom on phones and centers on wider screens. */
export function BottomSheet({ open, onClose, labelledBy, children }: Props) {
  const dialog = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const el = dialog.current;
    if (!el) return;
    if (open && !el.open) el.showModal();
    else if (!open && el.open) el.close();
  }, [open]);

  return (
    <dialog
      ref={dialog}
      onClose={onClose}
      aria-labelledby={labelledBy}
      className='m-0 mt-auto max-h-[92svh] w-full max-w-none bg-transparent p-0 backdrop:bg-black/45 sm:mx-auto sm:max-w-md'
    >
      <div className='flex flex-col gap-3.5 rounded-t-3xl bg-card px-5 pt-2.5 pb-[max(env(safe-area-inset-bottom),24px)] text-ink'>
        <div aria-hidden='true' className='h-1.5 w-10 self-center rounded-full bg-field' />
        {children}
      </div>
    </dialog>
  );
}
