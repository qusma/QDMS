import { useMemo, useState } from 'react';
import { api } from '../api';
import { useExchanges, useInvalidate, useTimezones } from '../queries';
import type { Exchange, ExchangeSession } from '../types';
import { confirmAction, ErrorBox, Field, TextInput } from '../ui';
import SessionsEditor, { blankSession } from '../components/SessionsEditor';

const blank = (): Exchange => ({ ID: 0, Name: '', LongName: '', Timezone: 'Eastern Standard Time', Sessions: [] });

export default function ExchangesPage() {
  const exchanges = useExchanges();
  const timezones = useTimezones();
  const invalidate = useInvalidate();
  const [filter, setFilter] = useState('');
  const [edit, setEdit] = useState<Exchange | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const list = useMemo(() => {
    const f = filter.toLowerCase();
    return (exchanges.data ?? [])
      .filter((e) => !f || e.Name.toLowerCase().includes(f) || e.LongName?.toLowerCase().includes(f))
      .sort((a, b) => a.Name.localeCompare(b.Name));
  }, [exchanges.data, filter]);

  const run = async (f: () => Promise<Exchange | null>) => {
    setBusy(true);
    setError(null);
    try {
      const res = await f();
      await invalidate('exchanges', 'instruments');
      setEdit(res);
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  };

  const save = () => edit && run(() => (edit.ID ? api.updateExchange(edit) : api.addExchange(edit)));
  const remove = () => {
    if (!edit?.ID || !confirmAction(`Delete exchange ${edit.Name}?`)) return;
    run(async () => { await api.deleteExchange(edit.ID); return null; });
  };

  return (
    <div className="page">
      <div className="page-head">
        <h1>Exchanges</h1>
        <span className="grow" />
        <button className="primary" onClick={() => setEdit(blank())}>Add exchange</button>
      </div>
      <ErrorBox error={exchanges.error} />
      <div className="split">
        <div className="list">
          <div style={{ padding: 8 }}><input placeholder="Filter…" value={filter} onChange={(e) => setFilter(e.target.value)} style={{ width: '100%' }} /></div>
          {list.map((e) => (
            <button key={e.ID} className={edit?.ID === e.ID ? 'active' : ''} onClick={() => { setError(null); setEdit(structuredClone(e)); }}>
              <b>{e.Name}</b> <span className="muted">{e.LongName}</span>
            </button>
          ))}
        </div>
        <div className="card" style={{ overflow: 'auto' }}>
          {!edit && <p className="muted">Pick an exchange.</p>}
          {edit && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div className="form">
                <Field label="Name"><TextInput value={edit.Name} onChange={(v) => setEdit({ ...edit, Name: v ?? '' })} /></Field>
                <Field label="Long name"><TextInput value={edit.LongName} onChange={(v) => setEdit({ ...edit, LongName: v ?? '' })} /></Field>
                <Field label="Time zone">
                  <select value={edit.Timezone ?? ''} onChange={(e) => setEdit({ ...edit, Timezone: e.target.value })}>
                    {!timezones.data?.some((t) => t.Id === edit.Timezone) && <option value={edit.Timezone}>{edit.Timezone}</option>}
                    {(timezones.data ?? []).map((t) => <option key={t.Id} value={t.Id}>{t.Id} — {t.DisplayName}</option>)}
                  </select>
                </Field>
              </div>
              <div>
                <h3 style={{ margin: '0 0 6px' }}>Sessions</h3>
                <p className="muted" style={{ margin: '0 0 8px' }}>Instruments that take their sessions from this exchange are updated when you save.</p>
                <SessionsEditor<ExchangeSession>
                  sessions={edit.Sessions ?? []}
                  onChange={(s) => setEdit({ ...edit, Sessions: s })}
                  newSession={() => ({ ...blankSession(), ExchangeID: edit.ID })}
                />
              </div>
              <ErrorBox error={error} />
              <div className="toolbar">
                <button className="primary" disabled={busy} onClick={save}>{edit.ID ? 'Save' : 'Add'}</button>
                {edit.ID !== 0 && <button className="danger" disabled={busy} onClick={remove}>Delete</button>}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
