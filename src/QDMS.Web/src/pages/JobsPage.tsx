import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api';
import { useInstruments, useTags } from '../queries';
import { BarSize, enumName, enumOptions, type DataUpdateJobSettings } from '../types';
import { confirmAction, EnumSelect, ErrorBox, Field, Modal, TextInput } from '../ui';

const blank = (): DataUpdateJobSettings => ({
  Name: '', UseTag: true, InstrumentID: null, Instrument: null, TagID: null, Tag: null, WeekDaysOnly: true, Time: '23:00:00', Frequency: BarSize.OneDay,
});

/** Scheduled data update jobs: download new bars for an instrument or all instruments with a tag, every day at a set time. */
export default function JobsPage() {
  const jobs = useQuery({ queryKey: ['jobs'], queryFn: api.dataUpdateJobs });
  const [edit, setEdit] = useState<{ job: DataUpdateJobSettings; original: DataUpdateJobSettings | null } | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<unknown>(null);

  const run = async (f: () => Promise<unknown>, ok?: string) => {
    setError(null);
    setMessage(null);
    try {
      await f();
      if (ok) setMessage(ok);
      await jobs.refetch();
    } catch (e) {
      setError(e);
    }
  };

  return (
    <div className="page">
      <div className="page-head">
        <h1>Data update jobs</h1>
        <span className="grow" />
        <button className="primary" onClick={() => setEdit({ job: blank(), original: null })}>Add job</button>
      </div>
      <ErrorBox error={jobs.error ?? error} />
      {message && <div className="notice">{message}</div>}
      <div className="card">
        <table className="simple">
          <thead><tr><th>Name</th><th>Updates</th><th>Frequency</th><th>Time</th><th>Days</th><th /></tr></thead>
          <tbody>
            {(jobs.data ?? []).map((j) => (
              <tr key={j.Name}>
                <td>{j.Name}</td>
                <td>{j.UseTag ? `Tag: ${j.Tag?.Name ?? j.TagID}` : `Instrument: ${j.Instrument?.Symbol ?? j.InstrumentID}`}</td>
                <td>{enumName(BarSize, j.Frequency)}</td>
                <td>{j.Time}</td>
                <td>{j.WeekDaysOnly ? 'Mon–Fri' : 'Every day'}</td>
                <td>
                  <div className="toolbar" style={{ justifyContent: 'flex-end' }}>
                    <button onClick={() => run(() => api.runJob('DataUpdate', j.Name), `Started ${j.Name}. The downloads show up in the log.`)}>Run now</button>
                    <button onClick={() => setEdit({ job: structuredClone(j), original: j })}>Edit</button>
                    <button className="danger" onClick={() => confirmAction(`Delete job ${j.Name}?`) && run(() => api.deleteDataUpdateJob(j))}>Delete</button>
                  </div>
                </td>
              </tr>
            ))}
            {jobs.data?.length === 0 && <tr><td colSpan={6} className="muted">No jobs</td></tr>}
          </tbody>
        </table>
      </div>
      {edit && (
        <JobEditor
          job={edit.job}
          isNew={edit.original === null}
          onClose={() => setEdit(null)}
          onSave={(job) =>
            run(async () => {
              // there's no update endpoint: replace the job
              if (edit.original) await api.deleteDataUpdateJob(edit.original);
              await api.addDataUpdateJob(job);
              setEdit(null);
            })
          }
        />
      )}
    </div>
  );
}

function JobEditor(props: { job: DataUpdateJobSettings; isNew: boolean; onClose: () => void; onSave: (j: DataUpdateJobSettings) => void }) {
  const [job, setJob] = useState(props.job);
  const instruments = useInstruments();
  const tags = useTags();
  const set = (patch: Partial<DataUpdateJobSettings>) => setJob((j) => ({ ...j, ...patch }));

  return (
    <Modal
      title={props.isNew ? 'New data update job' : `Edit ${props.job.Name}`}
      onClose={props.onClose}
      footer={
        <>
          <button onClick={props.onClose}>Cancel</button>
          <button className="primary" onClick={() => props.onSave(job)}>Save</button>
        </>
      }
    >
      <div className="form">
        <Field label="Name"><TextInput value={job.Name} onChange={(v) => set({ Name: v ?? '' })} /></Field>
        <Field label="Updates">
          <select value={job.UseTag ? 'tag' : 'instrument'} onChange={(e) => set({ UseTag: e.target.value === 'tag' })}>
            <option value="tag">All instruments with a tag</option>
            <option value="instrument">One instrument</option>
          </select>
        </Field>
        {job.UseTag ? (
          <Field label="Tag">
            <select
              value={job.TagID ?? ''}
              onChange={(e) => {
                const t = tags.data?.find((x) => x.ID === Number(e.target.value)) ?? null;
                set({ TagID: t?.ID ?? null, Tag: t });
              }}
            >
              <option value="">—</option>
              {(tags.data ?? []).map((t) => <option key={t.ID} value={t.ID}>{t.Name}</option>)}
            </select>
          </Field>
        ) : (
          <Field label="Instrument">
            <select
              value={job.InstrumentID ?? ''}
              onChange={(e) => {
                const i = instruments.data?.find((x) => x.ID === Number(e.target.value)) ?? null;
                set({ InstrumentID: i?.ID ?? null, Instrument: i });
              }}
            >
              <option value="">—</option>
              {[...(instruments.data ?? [])].sort((a, b) => a.Symbol.localeCompare(b.Symbol)).map((i) => (
                <option key={i.ID} value={i.ID!}>{i.Symbol} — {i.Name} ({i.Datasource?.Name})</option>
              ))}
            </select>
          </Field>
        )}
        <Field label="Frequency">
          <EnumSelect options={enumOptions(BarSize)} value={job.Frequency} onChange={(v) => set({ Frequency: (v ?? BarSize.OneDay) as BarSize })} />
        </Field>
        <Field label="Time (server local)">
          <input type="time" value={job.Time.slice(0, 5)} onChange={(e) => set({ Time: e.target.value + ':00' })} />
        </Field>
        <Field label="Weekdays only" check>
          <input type="checkbox" checked={job.WeekDaysOnly} onChange={(e) => set({ WeekDaysOnly: e.target.checked })} />
        </Field>
      </div>
    </Modal>
  );
}
