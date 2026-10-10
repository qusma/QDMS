import { useState } from 'react';
import { api } from '../api';
import { useInvalidate, useUnderlyingSymbols } from '../queries';
import {
  DayOfTheWeek, DayType, enumName, enumOptions, ReferenceDayType, RelativeMonth, WeekDayCount,
  type ExpirationRule, type UnderlyingSymbol,
} from '../types';
import { confirmAction, EnumSelect, ErrorBox, Field, NumberInput, TextInput } from '../ui';

const blankRule = (): ExpirationRule => ({
  DaysBefore: 0, DayType: DayType.Business, ReferenceRelativeMonth: RelativeMonth.CurrentMonth, ReferenceDayType: ReferenceDayType.WeekDays,
  ReferenceDays: 1, ReferenceWeekDayCount: WeekDayCount.Third, ReferenceWeekDay: DayOfTheWeek.Friday, ReferenceDayMustBeBusinessDay: true,
});

/** The rule in words, e.g. "0 business days before the third Friday of the contract month" */
function describe(r: ExpirationRule) {
  const month = r.ReferenceRelativeMonth === RelativeMonth.CurrentMonth ? 'the contract month'
    : r.ReferenceRelativeMonth === RelativeMonth.PreviousMonth ? 'the month before the contract month' : 'the month after the contract month';
  const ref = r.ReferenceDayType === ReferenceDayType.CalendarDays ? `day ${r.ReferenceDays} of ${month}`
    : r.ReferenceDayType === ReferenceDayType.WeekDays
      ? `the ${enumName(WeekDayCount, r.ReferenceWeekDayCount).toLowerCase()} ${enumName(DayOfTheWeek, r.ReferenceWeekDay)} of ${month}`
      : `the last business day of ${month}`;
  const before = r.DaysBefore === 0 ? 'On' : `${r.DaysBefore} ${enumName(DayType, r.DayType).toLowerCase()} day(s) before`;
  return `${before} ${ref}${r.ReferenceDayMustBeBusinessDay ? ' (moved back to a business day if it is a holiday)' : ''}.`;
}

export default function UnderlyingSymbolsPage() {
  const symbols = useUnderlyingSymbols();
  const invalidate = useInvalidate();
  const [edit, setEdit] = useState<UnderlyingSymbol | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const run = async (f: () => Promise<UnderlyingSymbol | null>) => {
    setBusy(true);
    setError(null);
    try {
      const res = await f();
      await invalidate('underlyingsymbols');
      setEdit(res);
    } catch (e) {
      setError(e);
    } finally {
      setBusy(false);
    }
  };

  const save = () => edit && run(() => (edit.ID ? api.updateUnderlyingSymbol(edit) : api.addUnderlyingSymbol(edit)));
  const remove = () => {
    if (!edit?.ID || !confirmAction(`Delete ${edit.Symbol}?`)) return;
    run(async () => { await api.deleteUnderlyingSymbol(edit.ID); return null; });
  };
  const setRule = (patch: Partial<ExpirationRule>) => edit && setEdit({ ...edit, Rule: { ...edit.Rule, ...patch } });

  return (
    <div className="page">
      <div className="page-head">
        <h1>Underlying symbols</h1>
        <span className="muted">Futures roots and their expiration rules, used by continuous futures.</span>
        <span className="grow" />
        <button className="primary" onClick={() => setEdit({ ID: 0, Symbol: '', Rule: blankRule() })}>Add symbol</button>
      </div>
      <ErrorBox error={symbols.error} />
      <div className="split">
        <div className="list">
          {[...(symbols.data ?? [])].sort((a, b) => a.Symbol.localeCompare(b.Symbol)).map((s) => (
            <button key={s.ID} className={edit?.ID === s.ID ? 'active' : ''} onClick={() => { setError(null); setEdit(structuredClone(s)); }}>
              {s.Symbol}
            </button>
          ))}
        </div>
        <div className="card">
          {!edit && <p className="muted">Pick a symbol.</p>}
          {edit && (
            <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
              <div className="form">
                <Field label="Symbol"><TextInput value={edit.Symbol} onChange={(v) => setEdit({ ...edit, Symbol: v ?? '' })} /></Field>
              </div>
              <h3 style={{ margin: 0 }}>Expiration rule</h3>
              <div className="form">
                <Field label="Reference month">
                  <EnumSelect options={enumOptions(RelativeMonth)} value={edit.Rule.ReferenceRelativeMonth} onChange={(v) => setRule({ ReferenceRelativeMonth: v ?? 0 })} />
                </Field>
                <Field label="Reference day">
                  <EnumSelect options={enumOptions(ReferenceDayType)} value={edit.Rule.ReferenceDayType} onChange={(v) => setRule({ ReferenceDayType: v ?? 0 })} />
                </Field>
                {edit.Rule.ReferenceDayType === ReferenceDayType.CalendarDays && (
                  <Field label="Day of month"><NumberInput step="1" value={edit.Rule.ReferenceDays} onChange={(v) => setRule({ ReferenceDays: v ?? 1 })} /></Field>
                )}
                {edit.Rule.ReferenceDayType === ReferenceDayType.WeekDays && (
                  <>
                    <Field label="Which">
                      <EnumSelect options={enumOptions(WeekDayCount)} value={edit.Rule.ReferenceWeekDayCount} onChange={(v) => setRule({ ReferenceWeekDayCount: v ?? 0 })} />
                    </Field>
                    <Field label="Weekday">
                      <EnumSelect options={enumOptions(DayOfTheWeek)} value={edit.Rule.ReferenceWeekDay} onChange={(v) => setRule({ ReferenceWeekDay: v ?? 0 })} />
                    </Field>
                  </>
                )}
                <Field label="Days before the reference day"><NumberInput step="1" value={edit.Rule.DaysBefore} onChange={(v) => setRule({ DaysBefore: v ?? 0 })} /></Field>
                <Field label="Counted in">
                  <EnumSelect options={enumOptions(DayType)} value={edit.Rule.DayType} onChange={(v) => setRule({ DayType: v ?? 0 })} />
                </Field>
                <Field label="Reference day must be a business day" check>
                  <input type="checkbox" checked={edit.Rule.ReferenceDayMustBeBusinessDay} onChange={(e) => setRule({ ReferenceDayMustBeBusinessDay: e.target.checked })} />
                </Field>
              </div>
              <div className="notice">{describe(edit.Rule)}</div>
              <ErrorBox error={error} />
              <div className="toolbar">
                <button className="primary" disabled={busy} onClick={save}>{edit.ID ? 'Save' : 'Add'}</button>
                {edit.ID !== 0 && <button className="danger" disabled={busy} onClick={remove}>Delete</button>}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
