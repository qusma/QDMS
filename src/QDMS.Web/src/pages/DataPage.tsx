import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { AgGridReact } from 'ag-grid-react';
import type { CellValueChangedEvent, ColDef, SelectionChangedEvent } from 'ag-grid-community';
import { api } from '../api';
import { useInvalidate, useStorageInfo } from '../queries';
import { BarSize, enumName, enumOptions, type OHLCBar } from '../types';
import { confirmAction, EnumSelect, ErrorBox, Field, formatDT, gridTheme } from '../ui';
import BarChart from '../components/BarChart';
import FetchModal from '../components/FetchModal';
import ImportModal from '../components/ImportModal';

const priceCol = (field: keyof OHLCBar, headerName?: string): ColDef<OHLCBar> => ({
  field, headerName, width: 110, editable: true, cellEditor: 'agNumberCellEditor', type: 'numericColumn',
  valueParser: (p) => (p.newValue === '' || p.newValue === null ? null : Number(p.newValue)),
});

function addDays(date: string, days: number) {
  const d = new Date(date.slice(0, 10) + 'T00:00:00Z');
  d.setUTCDate(d.getUTCDate() + days);
  return d.toISOString().slice(0, 10);
}

/** View and edit an instrument's stored bars. */
export default function DataPage() {
  const id = Number(useParams().id);
  const instrument = useQuery({ queryKey: ['instrument', id], queryFn: () => api.instrument(id) });
  const storage = useStorageInfo();
  const invalidate = useInvalidate();

  const stored = useMemo(() => (storage.data ?? []).filter((s) => s.InstrumentID === id), [storage.data, id]);
  const [frequency, setFrequency] = useState<BarSize | null>(null);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [adjusted, setAdjusted] = useState(false);
  const [recompute, setRecompute] = useState(true);
  const [bars, setBars] = useState<OHLCBar[]>([]);
  const [dirty, setDirty] = useState<Set<string>>(new Set());
  const [selected, setSelected] = useState<OHLCBar[]>([]);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [fetching, setFetching] = useState(false);
  const [importing, setImporting] = useState(false);

  // start on the daily data if there is any, else on the first stored frequency
  useEffect(() => {
    if (frequency !== null || stored.length === 0) return;
    const f = stored.find((s) => s.Frequency === BarSize.OneDay) ?? stored[0];
    setFrequency(f.Frequency);
    // intraday data can be big: start with the last 30 days
    if (f.Frequency < BarSize.OneDay) setFrom(addDays(f.LatestDate, -30));
  }, [stored, frequency]);

  const info = stored.find((s) => s.Frequency === frequency);
  const freq = frequency ?? BarSize.OneDay;

  const data = useQuery({
    queryKey: ['bars', id, freq, from, to],
    queryFn: () => api.bars(id, freq, from ? from + 'T00:00:00' : undefined, to ? to + 'T23:59:59' : undefined),
    enabled: frequency !== null,
    staleTime: 0,
  });

  useEffect(() => {
    if (data.data) {
      setBars(data.data.map((b) => ({ ...b })));
      setDirty(new Set());
    }
  }, [data.data]);

  const columns = useMemo<ColDef<OHLCBar>[]>(() => [
    { headerName: 'Date/time', width: 170, valueGetter: (p) => formatDT(p.data?.DT), sort: 'desc', comparator: (a, b) => (a < b ? -1 : a > b ? 1 : 0) },
    priceCol('Open'), priceCol('High'), priceCol('Low'), priceCol('Close'),
    priceCol('AdjOpen', 'Adj open'), priceCol('AdjHigh', 'Adj high'), priceCol('AdjLow', 'Adj low'), priceCol('AdjClose', 'Adj close'),
    priceCol('Volume'), priceCol('OpenInterest', 'Open int.'), priceCol('Dividend'), priceCol('Split'),
    { headerName: 'Opened', width: 170, valueGetter: (p) => formatDT(p.data?.DTOpen) },
  ], []);

  const reload = async () => {
    await invalidate('storageinfo');
    await data.refetch();
  };

  const run = async (f: () => Promise<unknown>) => {
    setBusy(true);
    setError(null);
    try {
      await f();
      await reload();
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  };

  const save = () => run(() => api.updateBars(id, freq, bars.filter((b) => dirty.has(b.DT)), recompute));

  const deleteSelected = () => {
    if (!confirmAction(`Delete ${selected.length} bar(s)?`)) return;
    run(() => api.deleteBars(id, freq, selected));
  };

  const deleteAll = () => {
    if (!confirmAction(`Delete ALL ${enumName(BarSize, freq)} data of ${instrument.data?.Symbol}?`)) return;
    run(async () => {
      await api.deleteAllBars(id, freq);
      setFrequency(null);
    });
  };

  const exportCsv = () => {
    const header = 'Date,Open,High,Low,Close,AdjOpen,AdjHigh,AdjLow,AdjClose,Volume,OpenInterest,Dividend,Split';
    const sorted = [...bars].sort((a, b) => (a.DT < b.DT ? -1 : 1));
    const lines = sorted.map((b) => [formatDT(b.DT), b.Open, b.High, b.Low, b.Close, b.AdjOpen, b.AdjHigh, b.AdjLow, b.AdjClose,
      b.Volume, b.OpenInterest, b.Dividend, b.Split].map((v) => v ?? '').join(','));
    const blob = new Blob([[header, ...lines].join('\n')], { type: 'text/csv' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `${instrument.data?.Symbol ?? id}-${enumName(BarSize, freq)}.csv`;
    a.click();
    URL.revokeObjectURL(a.href);
  };

  const inst = instrument.data;

  return (
    <div className="page">
      <div className="page-head">
        <Link to="/instruments">← Instruments</Link>
        <h1>{inst ? `${inst.Symbol} — ${inst.Name}` : '…'}</h1>
        <span className="muted">{inst?.Datasource?.Name}{inst?.Exchange ? ` · ${inst.Exchange.Name}` : ''}</span>
      </div>

      <ErrorBox error={instrument.error ?? data.error ?? error} />

      <div className="toolbar">
        <Field label="Frequency">
          <EnumSelect
            options={stored.length ? stored.map((s) => ({ label: enumName(BarSize, s.Frequency), value: s.Frequency })) : enumOptions(BarSize)}
            value={frequency}
            onChange={(v) => { setFrequency(v as BarSize); setFrom(''); setTo(''); }}
          />
        </Field>
        <Field label="From"><input type="date" value={from} onChange={(e) => setFrom(e.target.value)} /></Field>
        <Field label="To"><input type="date" value={to} onChange={(e) => setTo(e.target.value)} /></Field>
        <Field label="Chart adjusted prices" check>
          <input type="checkbox" checked={adjusted} onChange={(e) => setAdjusted(e.target.checked)} />
        </Field>
        <span className="grow" />
        {info && (
          <span className="muted">Stored: {formatDT(info.EarliestDate, true)} – {formatDT(info.LatestDate, true)}</span>
        )}
      </div>

      {stored.length === 0 && !storage.isPending && (
        <div className="notice">No data stored for this instrument yet. Use “Update data…” to download it, or import a file.</div>
      )}

      {bars.length > 0 && <BarChart bars={bars} adjusted={adjusted} />}

      <div className="toolbar">
        <button className="primary" disabled={busy || dirty.size === 0} onClick={save}>
          Save {dirty.size || ''} change{dirty.size === 1 ? '' : 's'}
        </button>
        <label className="toolbar" style={{ gap: 4 }} title="Recompute the adjusted prices after saving (needed when dividends or splits change)">
          <input type="checkbox" checked={recompute} onChange={(e) => setRecompute(e.target.checked)} />
          Recompute adjusted prices
        </label>
        <button disabled={busy || dirty.size === 0} onClick={() => data.data && (setBars(data.data.map((b) => ({ ...b }))), setDirty(new Set()))}>
          Discard changes
        </button>
        <span style={{ width: 12 }} />
        <button onClick={() => setFetching(true)} disabled={!inst}>Update data…</button>
        <button onClick={() => setImporting(true)} disabled={!inst}>Import…</button>
        <button onClick={exportCsv} disabled={bars.length === 0}>Export CSV</button>
        <span className="grow" />
        <button className="danger" disabled={busy || selected.length === 0} onClick={deleteSelected}>Delete {selected.length || ''} selected</button>
        <button className="danger" disabled={busy || !info} onClick={deleteAll}>Delete all {enumName(BarSize, freq)}</button>
      </div>

      <div className="grid-wrap">
        <AgGridReact<OHLCBar>
          theme={gridTheme}
          rowData={bars}
          columnDefs={columns}
          loading={data.isFetching}
          getRowId={(p) => p.data.DT}
          rowSelection={{ mode: 'multiRow', checkboxes: true, headerCheckbox: true, enableClickSelection: false }}
          onSelectionChanged={(e: SelectionChangedEvent<OHLCBar>) => setSelected(e.api.getSelectedRows())}
          onCellValueChanged={(e: CellValueChangedEvent<OHLCBar>) => e.data && setDirty((d) => new Set(d).add(e.data!.DT))}
          defaultColDef={{ sortable: true, resizable: true }}
          singleClickEdit
          stopEditingWhenCellsLoseFocus
        />
      </div>

      {fetching && inst && (
        <FetchModal instruments={[inst]} storage={storage.data ?? []} onClose={() => setFetching(false)} onDone={reload} />
      )}
      {importing && inst && (
        <ImportModal instrument={inst} frequency={freq} onClose={() => setImporting(false)} onDone={async (f) => { setFrequency(f); await reload(); }} />
      )}
    </div>
  );
}
