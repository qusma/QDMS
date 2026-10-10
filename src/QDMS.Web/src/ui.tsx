// Small shared UI pieces: modal, form fields, errors, grid theme, formatting.

import { useEffect, type ReactNode } from 'react';
import { themeQuartz } from 'ag-grid-community';
import { ApiError } from './api';

// AG Grid theme driven by the page's color tokens, so it follows light/dark mode.
export const gridTheme = themeQuartz.withParams({
  backgroundColor: 'var(--panel)',
  foregroundColor: 'var(--fg)',
  borderColor: 'var(--border)',
  headerBackgroundColor: 'var(--panel-2)',
  oddRowBackgroundColor: 'var(--panel)',
  accentColor: 'var(--accent)',
  browserColorScheme: 'inherit',
  fontFamily: 'inherit',
  fontSize: 13,
  rowHeight: 30,
  headerHeight: 34,
  spacing: 6,
  wrapperBorderRadius: 8,
});

export function Modal(props: {
  title: ReactNode;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
  size?: 'small' | 'large';
}) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') props.onClose();
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [props.onClose]);

  return (
    <div className="modal-back" onMouseDown={(e) => e.target === e.currentTarget && props.onClose()}>
      <div className={`modal ${props.size ?? ''}`} role="dialog" aria-modal="true">
        <header>
          <span className="grow">{props.title}</span>
          <button className="link" onClick={props.onClose} aria-label="Close">✕</button>
        </header>
        <div className="body">{props.children}</div>
        {props.footer && <footer>{props.footer}</footer>}
      </div>
    </div>
  );
}

export function Field(props: { label: string; children: ReactNode; wide?: boolean; check?: boolean; title?: string }) {
  return (
    <label className={`field${props.wide ? ' wide' : ''}${props.check ? ' check' : ''}`} title={props.title}>
      {props.check ? (
        <>
          {props.children}
          <span>{props.label}</span>
        </>
      ) : (
        <>
          <span>{props.label}</span>
          {props.children}
        </>
      )}
    </label>
  );
}

export function TextInput(props: { value: string | null | undefined; onChange: (v: string | null) => void; placeholder?: string; nullable?: boolean }) {
  return (
    <input
      value={props.value ?? ''}
      placeholder={props.placeholder}
      onChange={(e) => props.onChange(e.target.value === '' && props.nullable ? null : e.target.value)}
    />
  );
}

export function NumberInput(props: { value: number | null | undefined; onChange: (v: number | null) => void; step?: string }) {
  return (
    <input
      type="number"
      step={props.step ?? 'any'}
      value={props.value ?? ''}
      onChange={(e) => props.onChange(e.target.value === '' ? null : Number(e.target.value))}
    />
  );
}

export function EnumSelect(props: {
  options: { label: string; value: number }[];
  value: number | null | undefined;
  onChange: (v: number | null) => void;
  nullable?: boolean;
}) {
  return (
    <select
      value={props.value ?? ''}
      onChange={(e) => props.onChange(e.target.value === '' ? null : Number(e.target.value))}
    >
      {props.nullable && <option value="">—</option>}
      {props.options.map((o) => (
        <option key={o.value} value={o.value}>{o.label}</option>
      ))}
    </select>
  );
}

export function ErrorBox({ error }: { error: unknown }) {
  if (!error) return null;
  let text: string;
  if (error instanceof ApiError) {
    text = error.message + (error.details.length ? '\n' + error.details.join('\n') : '');
  } else if (error instanceof Error) {
    text = error.message;
  } else {
    text = String(error);
  }
  return <div className="error">{text}</div>;
}

/** "2021-03-12T00:00:00" → "2021-03-12" (daily bars) or "2021-03-12 15:30:00" */
export function formatDT(dt: string | null | undefined, dateOnly = false) {
  if (!dt) return '';
  const [d, t = ''] = dt.split('T');
  if (dateOnly || t.startsWith('00:00:00')) return d;
  return `${d} ${t.slice(0, 8)}`;
}

export const today = () => new Date().toISOString().slice(0, 10);

export function confirmAction(message: string) {
  return window.confirm(message);
}
