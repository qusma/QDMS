import { useState, type FormEvent } from 'react';
import { api } from '../api';
import { useDatasources, useExchanges, useInstruments } from '../queries';
import { BarSize, DataLocation, InstrumentType, type Instrument, type TiingoMeta, type TiingoSearchResult } from '../types';
import { ErrorBox, Field, Modal, today } from '../ui';
import InstrumentEditor, { newInstrument } from './InstrumentEditor';

// Tiingo's exchangeCode → our exchange names (US listings; the instrument itself goes on SMART like the existing ones)
const exchangeMap: Record<string, string> = {
  NYSE: 'NYSE',
  NASDAQ: 'NASDAQ',
  'NYSE ARCA': 'ARCA',
  NYSEARCA: 'ARCA',
  'NYSE MKT': 'AMEX',
  AMEX: 'AMEX',
  BATS: 'BATS',
};

/**
 * Add an instrument that gets its data from Tiingo: search a ticker, check the prefilled instrument,
 * save it, then optionally download its full daily history.
 */
export default function TiingoAddModal(props: { onClose: () => void; onAdded: () => void }) {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<TiingoSearchResult[] | null>(null);
  const [searching, setSearching] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [draft, setDraft] = useState<{ instrument: Instrument; meta: TiingoMeta } | null>(null);
  const [added, setAdded] = useState<{ instrument: Instrument; meta: TiingoMeta } | null>(null);
  const [download, setDownload] = useState<{ state: 'idle' | 'running' | 'done' | 'failed'; message: string }>({ state: 'idle', message: '' });

  const exchanges = useExchanges();
  const datasources = useDatasources();
  const instruments = useInstruments();

  const search = async (e: FormEvent) => {
    e.preventDefault();
    if (!query.trim()) return;
    setSearching(true);
    setError(null);
    try {
      setResults(await api.tiingoSearch(query.trim()));
    } catch (err) {
      setError(err);
    } finally {
      setSearching(false);
    }
  };

  const pick = async (r: TiingoSearchResult) => {
    setError(null);
    try {
      const meta = await api.tiingoMeta(r.Ticker);
      const tiingo = datasources.data?.find((d) => d.Name === 'Tiingo') ?? null;
      const smart = exchanges.data?.find((x) => x.Name === 'SMART') ?? null;
      const primary = exchanges.data?.find((x) => x.Name === exchangeMap[meta.ExchangeCode?.toUpperCase() ?? '']) ?? null;
      const instrument: Instrument = {
        ...newInstrument(),
        Symbol: r.Ticker.toUpperCase(),
        Name: meta.Name || r.Name,
        Type: r.AssetType === 'Mutual Fund' ? InstrumentType.Fund : InstrumentType.Stock,
        Currency: 'USD',
        Datasource: tiingo,
        DatasourceID: tiingo?.ID ?? null,
        Exchange: smart,
        ExchangeID: smart?.ID ?? null,
        PrimaryExchange: primary,
        PrimaryExchangeID: primary?.ID ?? null,
      };
      setDraft({ instrument, meta });
    } catch (err) {
      setError(err);
    }
  };

  const downloadHistory = async () => {
    if (!added) return;
    setDownload({ state: 'running', message: '' });
    try {
      const start = added.meta.StartDate?.slice(0, 10) || '1990-01-01';
      const res = await api.fetch(added.instrument.ID!, {
        Frequency: BarSize.OneDay,
        StartingDate: start + 'T00:00:00',
        EndingDate: today() + 'T23:59:59',
        DataLocation: DataLocation.ExternalOnly,
        SaveToStorage: true,
        RTHOnly: true,
        TimeoutSeconds: 300,
      });
      setDownload({ state: 'done', message: `${res.Bars.length} daily bars saved` + (res.Errors.length ? ` — ${res.Errors.join('; ')}` : '') });
      props.onAdded();
    } catch (err) {
      setDownload({ state: 'failed', message: err instanceof Error ? err.message : String(err) });
    }
  };

  if (draft) {
    return (
      <InstrumentEditor
        instrument={draft.instrument}
        title={`Add ${draft.instrument.Symbol} from Tiingo`}
        saveLabel="Add"
        onClose={() => setDraft(null)}
        onSaved={(saved) => {
          setAdded({ instrument: saved, meta: draft.meta });
          setDraft(null);
          props.onAdded();
        }}
      />
    );
  }

  if (added) {
    return (
      <Modal
        size="small"
        title={`Added ${added.instrument.Symbol}`}
        onClose={props.onClose}
        footer={
          <>
            <button disabled={download.state === 'running'} onClick={props.onClose}>Close</button>
            {download.state !== 'done' && (
              <button className="primary" disabled={download.state === 'running'} onClick={downloadHistory}>
                {download.state === 'running' ? 'Downloading…' : 'Download daily history'}
              </button>
            )}
          </>
        }
      >
        <p>
          Tiingo has daily prices for {added.instrument.Symbol} from {added.meta.StartDate?.slice(0, 10) || '?'} to{' '}
          {added.meta.EndDate?.slice(0, 10) || '?'}.
        </p>
        {download.state === 'done' && <p className="ok">{download.message}</p>}
        {download.state === 'failed' && <div className="error">{download.message}</div>}
      </Modal>
    );
  }

  const existing = new Set((instruments.data ?? []).map((i) => i.Symbol.toUpperCase()));

  return (
    <Modal title="Add from Tiingo" onClose={props.onClose} footer={<button onClick={props.onClose}>Close</button>}>
      <form className="toolbar" onSubmit={search}>
        <Field label="Ticker or name">
          <input autoFocus value={query} onChange={(e) => setQuery(e.target.value)} style={{ width: 280 }} />
        </Field>
        <button className="primary" type="submit" disabled={searching} style={{ alignSelf: 'flex-end' }}>
          {searching ? 'Searching…' : 'Search'}
        </button>
      </form>
      <ErrorBox error={error} />
      {results && (
        <table className="simple">
          <thead>
            <tr><th>Ticker</th><th>Name</th><th>Type</th><th>Country</th><th>Active</th><th /></tr>
          </thead>
          <tbody>
            {results.map((r) => (
              <tr key={r.PermaTicker || r.Ticker}>
                <td>{r.Ticker}</td>
                <td>{r.Name}</td>
                <td>{r.AssetType}</td>
                <td>{r.CountryCode}</td>
                <td>{r.IsActive ? 'yes' : <span className="muted">delisted</span>}</td>
                <td>
                  <button onClick={() => pick(r)}>{existing.has(r.Ticker.toUpperCase()) ? 'Add again…' : 'Add…'}</button>
                </td>
              </tr>
            ))}
            {results.length === 0 && <tr><td colSpan={6} className="muted">Nothing found</td></tr>}
          </tbody>
        </table>
      )}
    </Modal>
  );
}
