import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '../api';
import { ErrorBox } from '../ui';

const yesNo = (v: boolean | null) => (v === null ? <span className="muted">—</span> : v ? <span className="ok">connected</span> : <span className="bad">disconnected</span>);

export default function StatusPage() {
  const [lines, setLines] = useState(300);
  const [follow, setFollow] = useState(true);
  const status = useQuery({ queryKey: ['dsstatus'], queryFn: api.datasourceStatus, refetchInterval: 10_000 });
  const streams = useQuery({ queryKey: ['streams'], queryFn: api.activeStreams, refetchInterval: 10_000 });
  const logs = useQuery({ queryKey: ['logs', lines], queryFn: () => api.logs(lines), refetchInterval: follow ? 5_000 : false });

  return (
    <div className="page">
      <div className="page-head"><h1>Status & logs</h1></div>
      <ErrorBox error={status.error ?? streams.error ?? logs.error} />
      <div className="cards">
        <div className="card">
          <h3 style={{ marginTop: 0 }}>Data sources</h3>
          <table className="simple">
            <thead><tr><th>Name</th><th>Historical</th><th>Real time</th></tr></thead>
            <tbody>
              {(status.data ?? []).map((s) => (
                <tr key={s.Name}><td>{s.Name}</td><td>{yesNo(s.HistoricalConnected)}</td><td>{yesNo(s.RealtimeConnected)}</td></tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="card">
          <h3 style={{ marginTop: 0 }}>Real-time streams</h3>
          {streams.data?.length ? (
            <pre className="mono" style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{JSON.stringify(streams.data, null, 1)}</pre>
          ) : <p className="muted">None active.</p>}
        </div>
      </div>
      <div className="toolbar">
        <h3 style={{ margin: 0 }}>Log</h3>
        <span className="muted mono">{logs.data?.File}</span>
        <span className="grow" />
        <select value={lines} onChange={(e) => setLines(Number(e.target.value))}>
          {[100, 300, 1000, 5000].map((n) => <option key={n} value={n}>Last {n} lines</option>)}
        </select>
        <label className="toolbar" style={{ gap: 4 }}>
          <input type="checkbox" checked={follow} onChange={(e) => setFollow(e.target.checked)} /> Refresh every 5 s
        </label>
        <button onClick={() => logs.refetch()}>Refresh</button>
      </div>
      <div className="logs">
        {[...(logs.data?.Lines ?? [])].reverse().map((l, i) => {
          const level = /\s(TRACE|DEBUG|INFO|WARN|ERROR|FATAL)\s/.exec(l)?.[1] ?? '';
          return <div key={i} className={level}>{l}</div>;
        })}
      </div>
    </div>
  );
}
