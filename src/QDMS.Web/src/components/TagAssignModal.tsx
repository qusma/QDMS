import { useState } from 'react';
import { api } from '../api';
import { useTags } from '../queries';
import type { Instrument } from '../types';
import { ErrorBox, Field, Modal } from '../ui';

/** Add or remove a tag on several instruments at once. */
export default function TagAssignModal(props: { instruments: Instrument[]; onClose: () => void; onDone: () => void }) {
  const tags = useTags();
  const [tagId, setTagId] = useState<number | ''>('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const apply = async (add: boolean) => {
    const tag = tags.data?.find((t) => t.ID === tagId);
    if (!tag) return;
    setBusy(true);
    setError(null);
    try {
      for (const i of props.instruments) {
        const has = i.Tags?.some((t) => t.ID === tag.ID);
        if (add === Boolean(has)) continue;
        const newTags = add ? [...(i.Tags ?? []), tag] : i.Tags.filter((t) => t.ID !== tag.ID);
        await api.updateInstrument({ ...i, Tags: newTags });
      }
      props.onDone();
      props.onClose();
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal
      size="small"
      title={`Tags — ${props.instruments.length} instrument(s)`}
      onClose={props.onClose}
      footer={
        <>
          <button onClick={props.onClose}>Cancel</button>
          <button disabled={busy || tagId === ''} onClick={() => apply(false)}>Remove tag</button>
          <button className="primary" disabled={busy || tagId === ''} onClick={() => apply(true)}>Add tag</button>
        </>
      }
    >
      <Field label="Tag">
        <select value={tagId} onChange={(e) => setTagId(e.target.value === '' ? '' : Number(e.target.value))}>
          <option value="">—</option>
          {(tags.data ?? []).map((t) => <option key={t.ID} value={t.ID}>{t.Name}</option>)}
        </select>
      </Field>
      <ErrorBox error={error} />
    </Modal>
  );
}
