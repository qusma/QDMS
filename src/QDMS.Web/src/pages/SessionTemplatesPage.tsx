import { useState } from 'react';
import { api } from '../api';
import { useInvalidate, useSessionTemplates } from '../queries';
import type { SessionTemplate, TemplateSession } from '../types';
import { confirmAction, ErrorBox, Field, TextInput } from '../ui';
import SessionsEditor, { blankSession } from '../components/SessionsEditor';

export default function SessionTemplatesPage() {
  const templates = useSessionTemplates();
  const invalidate = useInvalidate();
  const [edit, setEdit] = useState<SessionTemplate | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const run = async (f: () => Promise<SessionTemplate | null>) => {
    setBusy(true);
    setError(null);
    try {
      const res = await f();
      await invalidate('sessiontemplates', 'instruments');
      setEdit(res);
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  };

  const save = () => edit && run(() => (edit.ID ? api.updateSessionTemplate(edit) : api.addSessionTemplate(edit)));
  const remove = () => {
    if (!edit?.ID || !confirmAction(`Delete template ${edit.Name}?`)) return;
    run(async () => { await api.deleteSessionTemplate(edit.ID); return null; });
  };

  return (
    <div className="page">
      <div className="page-head">
        <h1>Session templates</h1>
        <span className="grow" />
        <button className="primary" onClick={() => setEdit({ ID: 0, Name: '', Sessions: [] })}>Add template</button>
      </div>
      <ErrorBox error={templates.error} />
      <div className="split">
        <div className="list">
          {(templates.data ?? []).map((t) => (
            <button key={t.ID} className={edit?.ID === t.ID ? 'active' : ''} onClick={() => { setError(null); setEdit(structuredClone(t)); }}>
              {t.Name}
            </button>
          ))}
        </div>
        <div className="card" style={{ overflow: 'auto' }}>
          {!edit && <p className="muted">Pick a template.</p>}
          {edit && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div className="form">
                <Field label="Name"><TextInput value={edit.Name} onChange={(v) => setEdit({ ...edit, Name: v ?? '' })} /></Field>
              </div>
              <SessionsEditor<TemplateSession>
                sessions={edit.Sessions ?? []}
                onChange={(s) => setEdit({ ...edit, Sessions: s })}
                newSession={() => ({ ...blankSession(), TemplateID: edit.ID })}
              />
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
