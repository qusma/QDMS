import { useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { AgGridReact } from 'ag-grid-react';
import type { ColDef, SelectionChangedEvent } from 'ag-grid-community';
import { api } from '../api';
import { useInstruments, useInvalidate, useStorageInfo, useTags } from '../queries';
import { BarSize, enumName, InstrumentType, type Instrument, type StoredDataInfo } from '../types';
import { confirmAction, ErrorBox, formatDT, gridTheme } from '../ui';
import InstrumentEditor, { cloneInstrument, newInstrument } from '../components/InstrumentEditor';
import TiingoAddModal from '../components/TiingoAddModal';
import FetchModal from '../components/FetchModal';
import TagAssignModal from '../components/TagAssignModal';

interface Row extends Instrument {
  storage: StoredDataInfo[];
  latest: string | null;
  frequencies: string;
  tagNames: string;
}

export default function InstrumentsPage() {
  const instruments = useInstruments();
  const storage = useStorageInfo();
  const tags = useTags();
  const invalidate = useInvalidate();
  const navigate = useNavigate();
  const gridRef = useRef<AgGridReact<Row>>(null);

  const [search, setSearch] = useState('');
  const [tagFilter, setTagFilter] = useState<number | ''>('');
  const [selected, setSelected] = useState<Row[]>([]);
  const [editing, setEditing] = useState<Instrument | null>(null);
  const [addTiingo, setAddTiingo] = useState(false);
  const [fetching, setFetching] = useState<Instrument[] | null>(null);
  const [tagging, setTagging] = useState<Instrument[] | null>(null);
  const [error, setError] = useState<unknown>(null);

  const rows = useMemo<Row[]>(() => {
    const byInstrument = new Map<number, StoredDataInfo[]>();
    for (const s of storage.data ?? []) {
      const list = byInstrument.get(s.InstrumentID) ?? [];
      list.push(s);
      byInstrument.set(s.InstrumentID, list);
    }
    return (instruments.data ?? [])
      .filter((i) => tagFilter === '' || i.Tags?.some((t) => t.ID === tagFilter))
      .map((i) => {
        const st = (byInstrument.get(i.ID ?? -1) ?? []).sort((a, b) => b.Frequency - a.Frequency);
        const latest = st.reduce<string | null>((m, s) => (m === null || s.LatestDate > m ? s.LatestDate : m), null);
        return {
          ...i,
          storage: st,
          latest,
          frequencies: st.map((s) => enumName(BarSize, s.Frequency)).join(', '),
          tagNames: (i.Tags ?? []).map((t) => t.Name).join(', '),
        };
      });
  }, [instruments.data, storage.data, tagFilter]);

  const columns = useMemo<ColDef<Row>[]>(() => [
    { field: 'ID', width: 80, filter: 'agNumberColumnFilter' },
    { field: 'Symbol', width: 110, filter: true, pinned: 'left' },
    { field: 'UnderlyingSymbol', headerName: 'Underlying', width: 110, filter: true },
    { field: 'Name', flex: 1, minWidth: 180, filter: true },
    { headerName: 'Type', width: 110, valueGetter: (p) => enumName(InstrumentType, p.data?.Type), filter: true },
    { headerName: 'Datasource', width: 150, valueGetter: (p) => p.data?.Datasource?.Name ?? '', filter: true },
    { field: 'DatasourceSymbol', headerName: 'DS symbol', width: 110, filter: true },
    { headerName: 'Exchange', width: 110, valueGetter: (p) => p.data?.Exchange?.Name ?? '', filter: true },
    { field: 'Currency', width: 90, filter: true },
    { headerName: 'Expiration', width: 115, valueGetter: (p) => formatDT(p.data?.Expiration, true), filter: true },
    { field: 'Multiplier', width: 100 },
    { field: 'tagNames', headerName: 'Tags', width: 150, filter: true },
    { field: 'frequencies', headerName: 'Stored data', width: 150, filter: true },
    { headerName: 'Latest bar', width: 120, valueGetter: (p) => formatDT(p.data?.latest, true), sort: undefined, filter: true },
  ], []);

  const one = selected.length === 1 ? selected[0] : null;

  const remove = async () => {
    const names = selected.map((s) => s.Symbol).join(', ');
    if (!confirmAction(`Delete ${selected.length} instrument(s) (${names}) and all their stored data?`)) return;
    setError(null);
    try {
      for (const s of selected) await api.deleteInstrument(s.ID!);
    } catch (e) {
      setError(e);
    }
    await invalidate('instruments', 'storageinfo');
  };

  const load = instruments.error ?? storage.error ?? tags.error;

  return (
    <div className="page">
      <div className="page-head">
        <h1>Instruments</h1>
        <span className="muted">{rows.length} shown</span>
        <span className="grow" />
        <input placeholder="Search…" value={search} onChange={(e) => setSearch(e.target.value)} style={{ width: 220 }} />
        <select value={tagFilter} onChange={(e) => setTagFilter(e.target.value === '' ? '' : Number(e.target.value))}>
          <option value="">All tags</option>
          {(tags.data ?? []).map((t) => <option key={t.ID} value={t.ID}>{t.Name}</option>)}
        </select>
      </div>

      <div className="toolbar">
        <button className="primary" onClick={() => setEditing(newInstrument())}>Add manually</button>
        <button className="primary" onClick={() => setAddTiingo(true)}>Add from Tiingo</button>
        <span style={{ width: 12 }} />
        <button disabled={!one} onClick={() => one && setEditing(one)}>Edit</button>
        <button disabled={!one} onClick={() => one && setEditing(cloneInstrument(one))}>Clone</button>
        <button disabled={!one} onClick={() => one && navigate(`/instruments/${one.ID}/data`)}>Data</button>
        <button disabled={selected.length === 0} onClick={() => setFetching(selected)} title="Download data from the instruments' data source">
          Update data…
        </button>
        <button disabled={selected.length === 0} onClick={() => setTagging(selected)}>Tags…</button>
        <button className="danger" disabled={selected.length === 0} onClick={remove}>Delete</button>
        <span className="grow" />
        <button onClick={() => invalidate('instruments', 'storageinfo', 'tags')}>Refresh</button>
      </div>

      <ErrorBox error={load ?? error} />

      <div className="grid-wrap">
        <AgGridReact<Row>
          ref={gridRef}
          theme={gridTheme}
          rowData={rows}
          columnDefs={columns}
          loading={instruments.isPending}
          quickFilterText={search}
          getRowId={(p) => String(p.data.ID)}
          rowSelection={{ mode: 'multiRow', enableClickSelection: true, checkboxes: true, headerCheckbox: true }}
          selectionColumnDef={{ pinned: 'left', width: 44 }}
          onSelectionChanged={(e: SelectionChangedEvent<Row>) => setSelected(e.api.getSelectedRows())}
          onRowDoubleClicked={(e) => e.data && navigate(`/instruments/${e.data.ID}/data`)}
          defaultColDef={{ sortable: true, resizable: true }}
        />
      </div>

      {editing && (
        <InstrumentEditor
          instrument={editing}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            invalidate('instruments');
          }}
        />
      )}
      {addTiingo && (
        <TiingoAddModal
          onClose={() => setAddTiingo(false)}
          onAdded={() => invalidate('instruments', 'storageinfo')}
        />
      )}
      {fetching && (
        <FetchModal
          instruments={fetching}
          storage={storage.data ?? []}
          onClose={() => setFetching(null)}
          onDone={() => invalidate('storageinfo')}
        />
      )}
      {tagging && (
        <TagAssignModal
          instruments={tagging}
          onClose={() => setTagging(null)}
          onDone={() => invalidate('instruments')}
        />
      )}
    </div>
  );
}
