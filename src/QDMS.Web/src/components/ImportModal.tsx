import { useMemo, useState } from 'react';
import { api } from '../api';
import { BarSize, enumOptions, type Instrument, type OHLCBar } from '../types';
import { EnumSelect, ErrorBox, Field, Modal } from '../ui';

// Bar fields that can be imported, and the header names they're matched to automatically
const fields = [
  { key: 'Date', aliases: ['date', 'dt', 'datetime', 'timestamp', 'time'] },
  { key: 'Time', aliases: ['time'] },
  { key: 'Open', aliases: ['open', 'o'] },
  { key: 'High', aliases: ['high', 'h'] },
  { key: 'Low', aliases: ['low', 'l'] },
  { key: 'Close', aliases: ['close', 'c', 'last'] },
  { key: 'AdjClose', aliases: ['adj close', 'adjclose', 'adj_close', 'adjusted close'] },
  { key: 'Volume', aliases: ['volume', 'vol', 'v'] },
  { key: 'OpenInterest', aliases: ['openinterest', 'open interest', 'oi'] },
  { key: 'Dividend', aliases: ['dividend', 'dividends', 'divcash'] },
  { key: 'Split', aliases: ['split', 'splits', 'splitfactor'] },
] as const;
type FieldKey = (typeof fields)[number]['key'];

const dateFormats = ['yyyy-MM-dd', 'yyyyMMdd', 'MM/dd/yyyy', 'dd/MM/yyyy', 'dd.MM.yyyy', 'yyyy-MM-dd HH:mm:ss', 'yyyy-MM-ddTHH:mm:ss', 'yyyyMMdd HH:mm:ss'];

function splitLine(line: string, delim: string): string[] {
  // simple CSV: handles quoted fields without embedded newlines
  const out: string[] = [];
  let cur = '';
  let quoted = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (ch === '"') {
      if (quoted && line[i + 1] === '"') { cur += '"'; i++; } else quoted = !quoted;
    } else if (ch === delim && !quoted) {
      out.push(cur.trim());
      cur = '';
    } else cur += ch;
  }
  out.push(cur.trim());
  return out;
}

/** Parse a date string with a format made of yyyy MM dd HH mm ss → "yyyy-MM-ddTHH:mm:ss" */
function parseDate(value: string, format: string): string | null {
  const tokens = ['yyyy', 'MM', 'dd', 'HH', 'mm', 'ss'];
  const order: string[] = [];
  let pattern = '';
  for (let i = 0; i < format.length;) {
    const t = tokens.find((x) => format.startsWith(x, i));
    if (t) {
      pattern += t === 'yyyy' ? '(\\d{4})' : '(\\d{1,2})';
      order.push(t);
      i += t.length;
    } else {
      pattern += format[i].replace(/[.*+?^${}()|[\]\\/]/g, '\\$&');
      i++;
    }
  }
  const m = new RegExp('^' + pattern + '$').exec(value.trim());
  if (!m) return null;
  const parts: Record<string, string> = { yyyy: '1970', MM: '01', dd: '01', HH: '00', mm: '00', ss: '00' };
  order.forEach((t, i) => (parts[t] = m[i + 1].padStart(2, '0')));
  return `${parts.yyyy}-${parts.MM}-${parts.dd}T${parts.HH}:${parts.mm}:${parts.ss}`;
}

const num = (v: string | undefined) => {
  if (v === undefined || v.trim() === '') return null;
  const n = Number(v.replace(/,/g, ''));
  return Number.isFinite(n) ? n : null;
};

/** Import bars from a CSV file or pasted text. */
export default function ImportModal(props: {
  instrument: Instrument;
  frequency: BarSize;
  onClose: () => void;
  onDone: (frequency: BarSize) => void;
}) {
  const [text, setText] = useState('');
  const [frequency, setFrequency] = useState<BarSize>(props.frequency);
  const [hasHeader, setHasHeader] = useState(true);
  const [delimiter, setDelimiter] = useState('auto');
  const [dateFormat, setDateFormat] = useState('yyyy-MM-dd');
  const [mapping, setMapping] = useState<Partial<Record<FieldKey, number>>>({});
  const [overwrite, setOverwrite] = useState(false);
  const [adjust, setAdjust] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>(null);
  const [result, setResult] = useState<string | null>(null);

  const rows = useMemo(() => {
    const lines = text.split(/\r?\n/).filter((l) => l.trim() !== '');
    if (lines.length === 0) return { header: [] as string[], data: [] as string[][] };
    const delim = delimiter !== 'auto' ? delimiter
      : [',', ';', '\t', '|'].map((d) => ({ d, n: lines[0].split(d).length })).sort((a, b) => b.n - a.n)[0].d;
    const all = lines.map((l) => splitLine(l, delim));
    return hasHeader ? { header: all[0], data: all.slice(1) } : { header: all[0].map((_, i) => `Column ${i + 1}`), data: all };
  }, [text, delimiter, hasHeader]);

  // map columns by header name, unless set by hand
  const effective = useMemo(() => {
    const m: Partial<Record<FieldKey, number>> = {};
    const lower = rows.header.map((h) => h.toLowerCase().trim());
    for (const f of fields) {
      if (mapping[f.key] !== undefined) {
        if (mapping[f.key]! >= 0) m[f.key] = mapping[f.key];
        continue;
      }
      if (!hasHeader) continue;
      const idx = lower.findIndex((h) => (f.aliases as readonly string[]).includes(h));
      // "time" is only the Time column if there's also a separate date column
      if (idx >= 0 && !(f.key === 'Time' && m.Date === idx)) m[f.key] = idx;
    }
    return m;
  }, [rows.header, mapping, hasHeader]);

  const parsed = useMemo(() => {
    const bars: OHLCBar[] = [];
    const problems: string[] = [];
    rows.data.forEach((r, i) => {
      const get = (k: FieldKey) => (effective[k] !== undefined ? r[effective[k]!] : undefined);
      const dateText = [get('Date'), get('Time')].filter(Boolean).join(' ');
      const fmt = get('Time') ? dateFormat + ' HH:mm:ss' : dateFormat;
      const dt = parseDate(dateText, fmt) ?? parseDate(dateText, get('Time') ? dateFormat + ' HH:mm' : dateFormat);
      const open = num(get('Open')), high = num(get('High')), low = num(get('Low')), close = num(get('Close'));
      if (!dt || open === null || high === null || low === null || close === null) {
        if (problems.length < 5) problems.push(`Row ${i + 1 + (hasHeader ? 1 : 0)}: can't read ${!dt ? `date "${dateText}"` : 'prices'}`);
        return;
      }
      const adjClose = num(get('AdjClose'));
      // with only an adjusted close, the other adjusted prices use the same ratio
      const ratio = adjClose !== null && close !== 0 ? adjClose / close : null;
      bars.push({
        DT: dt, DTOpen: null, Open: open, High: high, Low: low, Close: close,
        AdjOpen: ratio !== null ? open * ratio : null,
        AdjHigh: ratio !== null ? high * ratio : null,
        AdjLow: ratio !== null ? low * ratio : null,
        AdjClose: adjClose,
        Volume: num(get('Volume')), OpenInterest: num(get('OpenInterest')), Dividend: num(get('Dividend')), Split: num(get('Split')),
        Frequency: frequency,
      });
    });
    return { bars, problems, skipped: rows.data.length - bars.length };
  }, [rows.data, effective, dateFormat, frequency, hasHeader]);

  const importBars = async () => {
    setBusy(true);
    setError(null);
    try {
      const res = await api.addBars(props.instrument.ID!, frequency, parsed.bars.map((b) => ({ ...b, InstrumentID: props.instrument.ID! })), overwrite, adjust);
      setResult(`Imported ${res.Count} bars.`);
      props.onDone(frequency);
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  };

  const loadFile = (file: File | undefined) => {
    if (!file) return;
    file.text().then(setText);
  };

  return (
    <Modal
      size="large"
      title={`Import data — ${props.instrument.Symbol}`}
      onClose={props.onClose}
      footer={
        <>
          <button onClick={props.onClose}>Close</button>
          <button className="primary" disabled={busy || parsed.bars.length === 0 || result !== null} onClick={importBars}>
            {busy ? 'Importing…' : `Import ${parsed.bars.length} bars`}
          </button>
        </>
      }
    >
      <div className="form">
        <Field label="File"><input type="file" accept=".csv,.txt,text/csv" onChange={(e) => loadFile(e.target.files?.[0])} /></Field>
        <Field label="Frequency">
          <EnumSelect options={enumOptions(BarSize)} value={frequency} onChange={(v) => setFrequency((v ?? BarSize.OneDay) as BarSize)} />
        </Field>
        <Field label="Date format">
          <input list="dateformats" value={dateFormat} onChange={(e) => setDateFormat(e.target.value)} />
          <datalist id="dateformats">{dateFormats.map((f) => <option key={f} value={f} />)}</datalist>
        </Field>
        <Field label="Delimiter">
          <select value={delimiter} onChange={(e) => setDelimiter(e.target.value)}>
            <option value="auto">Detect</option>
            <option value=",">Comma</option>
            <option value=";">Semicolon</option>
            <option value={'\t'}>Tab</option>
            <option value="|">Pipe</option>
          </select>
        </Field>
        <Field label="First row is a header" check><input type="checkbox" checked={hasHeader} onChange={(e) => setHasHeader(e.target.checked)} /></Field>
        <Field label="Overwrite existing bars" check><input type="checkbox" checked={overwrite} onChange={(e) => setOverwrite(e.target.checked)} /></Field>
        <Field label="Compute adjusted prices" check><input type="checkbox" checked={adjust} onChange={(e) => setAdjust(e.target.checked)} /></Field>
      </div>

      <Field label="Or paste the data" wide>
        <textarea rows={6} value={text} onChange={(e) => { setText(e.target.value); setResult(null); }} placeholder="Date,Open,High,Low,Close,Volume" />
      </Field>

      {rows.header.length > 0 && (
        <>
          <div className="form">
            {fields.map((f) => (
              <Field key={f.key} label={f.key}>
                <select
                  value={effective[f.key] ?? -1}
                  onChange={(e) => setMapping((m) => ({ ...m, [f.key]: Number(e.target.value) }))}
                >
                  <option value={-1}>—</option>
                  {rows.header.map((h, i) => <option key={i} value={i}>{h}</option>)}
                </select>
              </Field>
            ))}
          </div>

          <div className="muted">
            {parsed.bars.length} bars read{parsed.skipped ? `, ${parsed.skipped} rows skipped` : ''}.
            {parsed.bars.length > 0 && ` ${parsed.bars[0].DT.slice(0, 10)} … ${parsed.bars[parsed.bars.length - 1].DT.slice(0, 10)}`}
          </div>
          {parsed.problems.length > 0 && <div className="error">{parsed.problems.join('\n')}</div>}

          <table className="simple mono">
            <thead><tr><th>DT</th><th>Open</th><th>High</th><th>Low</th><th>Close</th><th>AdjClose</th><th>Volume</th><th>Div</th><th>Split</th></tr></thead>
            <tbody>
              {parsed.bars.slice(0, 8).map((b) => (
                <tr key={b.DT}>
                  <td>{b.DT.replace('T', ' ')}</td><td>{b.Open}</td><td>{b.High}</td><td>{b.Low}</td><td>{b.Close}</td>
                  <td>{b.AdjClose ?? ''}</td><td>{b.Volume ?? ''}</td><td>{b.Dividend ?? ''}</td><td>{b.Split ?? ''}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}

      {result && <div className="notice ok">{result}</div>}
      <ErrorBox error={error} />
    </Modal>
  );
}
