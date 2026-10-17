import { useState } from 'react';
import { api } from '../api';
import { useInstruments, useInvalidate, useTags } from '../queries';
import type { Tag } from '../types';
import { confirmAction, ErrorBox } from '../ui';

export default function TagsPage() {
  const tags = useTags();
  const instruments = useInstruments();
  const invalidate = useInvalidate();
  const [name, setName] = useState('');
  const [renaming, setRenaming] = useState<Tag | null>(null);
  const [error, setError] = useState<unknown>(null);

  const run = async (f: () => Promise<unknown>) => {
    setError(null);
    try {
      await f();
      await invalidate('tags', 'instruments');
    } catch (e) {
      setError(e);
    }
  };

  const count = (t: Tag) => (instruments.data ?? []).filter((i) => i.Tags?.some((x) => x.ID === t.ID)).length;

  return (
    <div className="page">
      <div className="page-head"><h1>Tags</h1></div>
      <form
        className="toolbar"
        onSubmit={(e) => {
          e.preventDefault();
          if (name.trim()) run(async () => { await api.addTag({ ID: 0, Name: name.trim() }); setName(''); });
        }}
      >
        <input placeholder="New tag" value={name} onChange={(e) => setName(e.target.value)} />
        <button className="primary" type="submit">Add</button>
      </form>
      <ErrorBox error={tags.error ?? error} />
      <div className="card" style={{ maxWidth: 640 }}>
        <table className="simple">
          <thead><tr><th>Name</th><th>Instruments</th><th /></tr></thead>
          <tbody>
            {(tags.data ?? []).map((t) => (
              <tr key={t.ID}>
                <td>
                  {renaming?.ID === t.ID ? (
                    <form
                      className="toolbar"
                      onSubmit={(e) => { e.preventDefault(); run(async () => { await api.updateTag(renaming); setRenaming(null); }); }}
                    >
                      <input autoFocus value={renaming.Name} onChange={(e) => setRenaming({ ...renaming, Name: e.target.value })} />
                      <button type="submit">Save</button>
                      <button type="button" onClick={() => setRenaming(null)}>Cancel</button>
                    </form>
                  ) : t.Name}
                </td>
                <td>{count(t)}</td>
                <td style={{ textAlign: 'right' }}>
                  <div className="toolbar" style={{ justifyContent: 'flex-end' }}>
                    <button onClick={() => setRenaming({ ...t })}>Rename</button>
                    <button className="danger" onClick={() => confirmAction(`Delete tag ${t.Name}?`) && run(() => api.deleteTag(t.ID))}>Delete</button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
