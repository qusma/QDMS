import { useState } from 'react';
import { api, ApiError } from '../api';
import { BarSize, DataLocation, enumOptions, type Instrument, type StoredDataInfo } from '../types';
import { EnumSelect, Field, Modal, today } from '../ui';

interface Status {
  symbol: string;
  state: 'waiting' | 'running' | 'done' | 'failed';
  message: string;
}

/**
 * Download data for one or more instruments from their data sources (through the historical data broker),
 * either since the last stored bar or for a given range. Requests run one after another.
 */
export default function FetchModal(props: {
  instruments: Instrument[];
  storage: StoredDataInfo[];
  onClose: () => void;
  onDone: () => void;
}) {
  const [frequency, setFrequency] = useState<BarSize>(BarSize.OneDay);
  const [mode, setMode] = useState<'since' | 'range'>('since');
  const [from, setFrom] = useState('2000-01-01');
  const [to, setTo] = useState(today());
  const [save, setSave] = useState(true);
  const [rthOnly, setRthOnly] = useState(true);
  const [running, setRunning] = useState(false);
  const [statuses, setStatuses] = useState<Status[]>([]);

  const latest = (i: Instrument) =>
    props.storage.find((s) => s.InstrumentID === i.ID && s.Frequency === frequency)?.LatestDate ?? null;

  const run = async () => {
    setRunning(true);
    const list: Status[] = props.instruments.map((i) => ({ symbol: i.Symbol, state: 'waiting', message: '' }));
    setStatuses([...list]);
    for (let n = 0; n < props.instruments.length; n++) {
      const inst = props.instruments[n];
      list[n] = { ...list[n], state: 'running' };
      setStatuses([...list]);

      const start = mode === 'since' ? (latest(inst)?.slice(0, 10) ?? from) : from;
      try {
        const res = await api.fetch(inst.ID!, {
          Frequency: frequency,
          StartingDate: start + 'T00:00:00',
          EndingDate: to + 'T23:59:59',
          DataLocation: DataLocation.ExternalOnly,
          SaveToStorage: save,
          RTHOnly: rthOnly,
          TimeoutSeconds: 300,
        });
        list[n] = {
          ...list[n],
          state: 'done',
          message: `${res.Bars.length} bars from ${start}` + (res.Errors.length ? ` — ${res.Errors.join('; ')}` : ''),
        };
      } catch (e) {
        const msg = e instanceof ApiError ? [e.message, ...e.details].filter(Boolean).join(': ') : String(e);
        list[n] = { ...list[n], state: 'failed', message: msg };
      }
      setStatuses([...list]);
    }
    setRunning(false);
    props.onDone();
  };

  return (
    <Modal
      title={`Update data — ${props.instruments.length === 1 ? props.instruments[0].Symbol : `${props.instruments.length} instruments`}`}
      onClose={() => !running && props.onClose()}
      footer={
        <>
          <button disabled={running} onClick={props.onClose}>Close</button>
          <button className="primary" disabled={running} onClick={run}>{running ? 'Downloading…' : 'Download'}</button>
        </>
      }
    >
      <div className="form">
        <Field label="Frequency">
          <EnumSelect options={enumOptions(BarSize)} value={frequency} onChange={(v) => setFrequency((v ?? BarSize.OneDay) as BarSize)} />
        </Field>
        <Field label="Period">
          <select value={mode} onChange={(e) => setMode(e.target.value as 'since' | 'range')}>
            <option value="since">Since the last stored bar</option>
            <option value="range">Date range</option>
          </select>
        </Field>
        <Field label={mode === 'since' ? 'From (if nothing is stored)' : 'From'}>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </Field>
        <Field label="To"><input type="date" value={to} onChange={(e) => setTo(e.target.value)} /></Field>
        <Field label="Save to local storage" check>
          <input type="checkbox" checked={save} onChange={(e) => setSave(e.target.checked)} />
        </Field>
        <Field label="Regular trading hours only" check>
          <input type="checkbox" checked={rthOnly} onChange={(e) => setRthOnly(e.target.checked)} />
        </Field>
      </div>

      {statuses.length > 0 && (
        <table className="simple">
          <thead><tr><th>Symbol</th><th>Status</th><th>Result</th></tr></thead>
          <tbody>
            {statuses.map((s, i) => (
              <tr key={i}>
                <td>{s.symbol}</td>
                <td className={s.state === 'failed' ? 'bad' : s.state === 'done' ? 'ok' : 'muted'}>{s.state}</td>
                <td>{s.message}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Modal>
  );
}
