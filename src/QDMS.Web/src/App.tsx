import { useState, type FormEvent } from 'react';
import { NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api, ApiError, getApiKey, setApiKey } from './api';
import { ErrorBox } from './ui';
import InstrumentsPage from './pages/InstrumentsPage';
import DataPage from './pages/DataPage';
import ExchangesPage from './pages/ExchangesPage';
import SessionTemplatesPage from './pages/SessionTemplatesPage';
import TagsPage from './pages/TagsPage';
import UnderlyingSymbolsPage from './pages/UnderlyingSymbolsPage';
import JobsPage from './pages/JobsPage';
import StatusPage from './pages/StatusPage';

export default function App() {
  const [key, setKey] = useState(getApiKey());
  // checks the stored key; a 401 sends us to the login form
  const check = useQuery({ queryKey: ['auth', key], queryFn: api.ping, enabled: key !== '' });

  if (key === '' || (check.error instanceof ApiError && check.error.status === 401)) {
    return <Login invalid={key !== ''} onLogin={(k) => { setApiKey(k); setKey(k); }} />;
  }
  if (check.isPending) return <div className="login spinner">Connecting…</div>;
  if (check.error) {
    return (
      <div className="login">
        <div className="card">
          <p>Can't reach the QDMS server.</p>
          <ErrorBox error={check.error} />
          <p><button onClick={() => check.refetch()}>Retry</button></p>
        </div>
      </div>
    );
  }

  return (
    <div className="app">
      <nav className="nav">
        <div className="brand">QDMS</div>
        <NavLink to="/instruments">Instruments</NavLink>
        <NavLink to="/jobs">Update jobs</NavLink>
        <NavLink to="/exchanges">Exchanges</NavLink>
        <NavLink to="/sessiontemplates">Session templates</NavLink>
        <NavLink to="/underlyingsymbols">Underlying symbols</NavLink>
        <NavLink to="/tags">Tags</NavLink>
        <NavLink to="/status">Status & logs</NavLink>
        <div className="spacer" />
        <button className="link small" onClick={() => { setApiKey(''); setKey(''); }}>Log out</button>
      </nav>
      <main className="main">
        <Routes>
          <Route path="/" element={<Navigate to="/instruments" replace />} />
          <Route path="/instruments" element={<InstrumentsPage />} />
          <Route path="/instruments/:id/data" element={<DataPage />} />
          <Route path="/jobs" element={<JobsPage />} />
          <Route path="/exchanges" element={<ExchangesPage />} />
          <Route path="/sessiontemplates" element={<SessionTemplatesPage />} />
          <Route path="/underlyingsymbols" element={<UnderlyingSymbolsPage />} />
          <Route path="/tags" element={<TagsPage />} />
          <Route path="/status" element={<StatusPage />} />
          <Route path="*" element={<Navigate to="/instruments" replace />} />
        </Routes>
      </main>
    </div>
  );
}

function Login(props: { invalid: boolean; onLogin: (key: string) => void }) {
  const [value, setValue] = useState('');
  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (value.trim()) props.onLogin(value.trim());
  };
  return (
    <div className="login">
      <form className="card" onSubmit={submit}>
        <h2 style={{ margin: 0 }}>QDMS</h2>
        {props.invalid && <div className="error">That API key was rejected.</div>}
        <label className="field">
          <span>API key (Qdms:apiKey in the server's settings)</span>
          <input type="password" autoFocus value={value} onChange={(e) => setValue(e.target.value)} />
        </label>
        <button className="primary" type="submit">Connect</button>
      </form>
    </div>
  );
}
