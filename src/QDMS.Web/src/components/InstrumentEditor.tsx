import { useState } from 'react';
import { api } from '../api';
import { useCurrencies, useDatasources, useExchanges, useSessionTemplates, useTags, useUnderlyingSymbols } from '../queries';
import {
  AdjustmentMode, enumOptions, InstrumentType, OptionType, RolloverType, SessionsSource,
  type ContinuousFuture, type Instrument, type InstrumentSession,
} from '../types';
import { EnumSelect, ErrorBox, Field, Modal, NumberInput, TextInput } from '../ui';
import SessionsEditor, { blankSession } from './SessionsEditor';

const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'] as const;

export function newInstrument(): Instrument {
  return {
    ID: null, Symbol: '', UnderlyingSymbol: null, Name: '', Type: InstrumentType.Stock, Multiplier: 1, Expiration: null,
    OptionType: null, Strike: null, Currency: 'USD', MinTick: null, Industry: null, Category: null, Subcategory: null,
    IsContinuousFuture: false, ValidExchanges: null, DatasourceSymbol: null, TradingClass: null, ExchangeID: null, Exchange: null,
    PrimaryExchangeID: null, PrimaryExchange: null, DatasourceID: null, Datasource: null, Tags: [],
    SessionsSource: SessionsSource.Exchange, SessionTemplateID: null, Sessions: [], ContinuousFutureID: null, ContinuousFuture: null,
  };
}

export function cloneInstrument(i: Instrument): Instrument {
  return {
    ...i,
    ID: null,
    Symbol: i.Symbol,
    Sessions: (i.Sessions ?? []).map((s) => ({ ...s, ID: 0, InstrumentID: 0 })),
    ContinuousFutureID: null,
    ContinuousFuture: i.ContinuousFuture ? { ...i.ContinuousFuture, ID: 0, InstrumentID: 0 } : null,
  };
}

function newContinuousFuture(): ContinuousFuture {
  return {
    ID: 0, InstrumentID: 0, UnderlyingSymbolID: 0, UnderlyingSymbol: null, Month: 1, RolloverType: RolloverType.Time, RolloverDays: 5,
    AdjustmentMode: AdjustmentMode.NoAdjustment,
    UseJan: true, UseFeb: true, UseMar: true, UseApr: true, UseMay: true, UseJun: true,
    UseJul: true, UseAug: true, UseSep: true, UseOct: true, UseNov: true, UseDec: true,
  };
}

export default function InstrumentEditor(props: {
  instrument: Instrument;
  onClose: () => void;
  onSaved: (saved: Instrument) => void;
  title?: string;
  saveLabel?: string;
}) {
  const [inst, setInst] = useState<Instrument>(props.instrument);
  const [tab, setTab] = useState<'general' | 'sessions' | 'cf'>('general');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<unknown>(null);

  const exchanges = useExchanges();
  const datasources = useDatasources();
  const tags = useTags();
  const templates = useSessionTemplates();
  const underlyings = useUnderlyingSymbols();
  const currencies = useCurrencies();

  const set = (patch: Partial<Instrument>) => setInst((i) => ({ ...i, ...patch }));
  const setCf = (patch: Partial<ContinuousFuture>) =>
    setInst((i) => ({ ...i, ContinuousFuture: { ...(i.ContinuousFuture ?? newContinuousFuture()), ...patch } }));

  const isNew = inst.ID === null;
  const exchange = exchanges.data?.find((e) => e.ID === inst.ExchangeID) ?? null;
  const template = templates.data?.find((t) => t.ID === inst.SessionTemplateID) ?? null;

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      const body: Instrument = {
        ...inst,
        ContinuousFuture: inst.IsContinuousFuture ? inst.ContinuousFuture ?? newContinuousFuture() : null,
        Sessions: inst.SessionsSource === SessionsSource.Custom ? inst.Sessions : [],
      };
      const saved = isNew ? await api.addInstrument(body) : await api.updateInstrument(body);
      props.onSaved(saved);
    } catch (e) {
      setError(e);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={props.title ?? (isNew ? 'New instrument' : `Edit ${props.instrument.Symbol}`)}
      onClose={props.onClose}
      footer={
        <>
          <button onClick={props.onClose}>Cancel</button>
          <button className="primary" disabled={saving} onClick={save}>{saving ? 'Saving…' : (props.saveLabel ?? 'Save')}</button>
        </>
      }
    >
      <div className="tabs">
        <button className={tab === 'general' ? 'active' : ''} onClick={() => setTab('general')}>General</button>
        <button className={tab === 'sessions' ? 'active' : ''} onClick={() => setTab('sessions')}>Sessions</button>
        <button className={tab === 'cf' ? 'active' : ''} onClick={() => setTab('cf')}>Continuous future</button>
      </div>

      {tab === 'general' && (
        <div className="form">
          <Field label="Symbol"><TextInput value={inst.Symbol} onChange={(v) => set({ Symbol: v ?? '' })} /></Field>
          <Field label="Name"><TextInput value={inst.Name} onChange={(v) => set({ Name: v ?? '' })} /></Field>
          <Field label="Underlying symbol"><TextInput nullable value={inst.UnderlyingSymbol} onChange={(v) => set({ UnderlyingSymbol: v })} /></Field>
          <Field label="Type">
            <EnumSelect options={enumOptions(InstrumentType)} value={inst.Type} onChange={(v) => set({ Type: (v ?? 0) as InstrumentType })} />
          </Field>
          <Field label="Datasource">
            <select
              value={inst.DatasourceID ?? ''}
              onChange={(e) => {
                const ds = datasources.data?.find((d) => d.ID === Number(e.target.value)) ?? null;
                set({ DatasourceID: ds?.ID ?? null, Datasource: ds });
              }}
            >
              <option value="">—</option>
              {(datasources.data ?? []).map((d) => <option key={d.ID} value={d.ID}>{d.Name}</option>)}
            </select>
          </Field>
          <Field label="Datasource symbol" title="The symbol at the data source, if it differs">
            <TextInput nullable value={inst.DatasourceSymbol} onChange={(v) => set({ DatasourceSymbol: v })} />
          </Field>
          <Field label="Exchange">
            <select
              value={inst.ExchangeID ?? ''}
              onChange={(e) => {
                const ex = exchanges.data?.find((x) => x.ID === Number(e.target.value)) ?? null;
                set({ ExchangeID: ex?.ID ?? null, Exchange: ex });
              }}
            >
              <option value="">—</option>
              {(exchanges.data ?? []).map((x) => <option key={x.ID} value={x.ID}>{x.Name} — {x.LongName}</option>)}
            </select>
          </Field>
          <Field label="Primary exchange">
            <select
              value={inst.PrimaryExchangeID ?? ''}
              onChange={(e) => {
                const ex = exchanges.data?.find((x) => x.ID === Number(e.target.value)) ?? null;
                set({ PrimaryExchangeID: ex?.ID ?? null, PrimaryExchange: ex });
              }}
            >
              <option value="">—</option>
              {(exchanges.data ?? []).map((x) => <option key={x.ID} value={x.ID}>{x.Name} — {x.LongName}</option>)}
            </select>
          </Field>
          <Field label="Currency">
            <input list="currencies" value={inst.Currency ?? ''} onChange={(e) => set({ Currency: e.target.value || null })} />
            <datalist id="currencies">
              {(currencies.data ?? []).map((c) => <option key={c.Id} value={c.Code}>{c.Name}</option>)}
            </datalist>
          </Field>
          <Field label="Multiplier"><NumberInput step="1" value={inst.Multiplier} onChange={(v) => set({ Multiplier: v })} /></Field>
          <Field label="Min tick"><NumberInput value={inst.MinTick} onChange={(v) => set({ MinTick: v })} /></Field>
          <Field label="Expiration">
            <input type="date" value={inst.Expiration?.slice(0, 10) ?? ''} onChange={(e) => set({ Expiration: e.target.value ? e.target.value + 'T00:00:00' : null })} />
          </Field>
          <Field label="Option type">
            <EnumSelect nullable options={enumOptions(OptionType)} value={inst.OptionType} onChange={(v) => set({ OptionType: v as OptionType | null })} />
          </Field>
          <Field label="Strike"><NumberInput value={inst.Strike} onChange={(v) => set({ Strike: v })} /></Field>
          <Field label="Trading class"><TextInput nullable value={inst.TradingClass} onChange={(v) => set({ TradingClass: v })} /></Field>
          <Field label="Valid exchanges"><TextInput nullable value={inst.ValidExchanges} onChange={(v) => set({ ValidExchanges: v })} /></Field>
          <Field label="Industry"><TextInput nullable value={inst.Industry} onChange={(v) => set({ Industry: v })} /></Field>
          <Field label="Category"><TextInput nullable value={inst.Category} onChange={(v) => set({ Category: v })} /></Field>
          <Field label="Subcategory"><TextInput nullable value={inst.Subcategory} onChange={(v) => set({ Subcategory: v })} /></Field>
          <div className="field wide">
            <span>Tags</span>
            <div className="toolbar">
              {(tags.data ?? []).map((t) => {
                const on = inst.Tags?.some((x) => x.ID === t.ID) ?? false;
                return (
                  <label key={t.ID} className="toolbar" style={{ gap: 4 }}>
                    <input
                      type="checkbox"
                      checked={on}
                      onChange={() => set({ Tags: on ? inst.Tags.filter((x) => x.ID !== t.ID) : [...(inst.Tags ?? []), t] })}
                    />
                    {t.Name}
                  </label>
                );
              })}
              {tags.data?.length === 0 && <span className="muted">No tags defined</span>}
            </div>
          </div>
        </div>
      )}

      {tab === 'sessions' && (
        <>
          <div className="form">
            <Field label="Sessions come from">
              <EnumSelect
                options={enumOptions(SessionsSource)}
                value={inst.SessionsSource}
                onChange={(v) => set({ SessionsSource: (v ?? 0) as SessionsSource })}
              />
            </Field>
            {inst.SessionsSource === SessionsSource.Template && (
              <Field label="Template">
                <select value={inst.SessionTemplateID ?? ''} onChange={(e) => set({ SessionTemplateID: e.target.value ? Number(e.target.value) : null })}>
                  <option value="">—</option>
                  {(templates.data ?? []).map((t) => <option key={t.ID} value={t.ID}>{t.Name}</option>)}
                </select>
              </Field>
            )}
          </div>
          {inst.SessionsSource === SessionsSource.Exchange && (
            exchange
              ? <SessionsEditor readOnly sessions={exchange.Sessions ?? []} onChange={() => {}} newSession={blankSession} />
              : <p className="muted">Pick an exchange on the General tab.</p>
          )}
          {inst.SessionsSource === SessionsSource.Template && template && (
            <SessionsEditor readOnly sessions={template.Sessions ?? []} onChange={() => {}} newSession={blankSession} />
          )}
          {inst.SessionsSource === SessionsSource.Custom && (
            <SessionsEditor<InstrumentSession>
              sessions={inst.Sessions ?? []}
              onChange={(s) => set({ Sessions: s })}
              newSession={() => ({ ...blankSession(), InstrumentID: inst.ID ?? 0 })}
            />
          )}
        </>
      )}

      {tab === 'cf' && (
        <>
          <Field label="This is a continuous future" check>
            <input type="checkbox" checked={inst.IsContinuousFuture} onChange={(e) => set({ IsContinuousFuture: e.target.checked })} />
          </Field>
          {inst.IsContinuousFuture && (() => {
            const cf = inst.ContinuousFuture ?? newContinuousFuture();
            return (
              <div className="form">
                <Field label="Underlying symbol">
                  <select
                    value={cf.UnderlyingSymbolID || ''}
                    onChange={(e) => {
                      const u = underlyings.data?.find((x) => x.ID === Number(e.target.value)) ?? null;
                      setCf({ UnderlyingSymbolID: u?.ID ?? 0, UnderlyingSymbol: u });
                    }}
                  >
                    <option value="">—</option>
                    {(underlyings.data ?? []).map((u) => <option key={u.ID} value={u.ID}>{u.Symbol}</option>)}
                  </select>
                </Field>
                <Field label="Month (1 = front)"><NumberInput step="1" value={cf.Month} onChange={(v) => setCf({ Month: v ?? 1 })} /></Field>
                <Field label="Rollover type">
                  <EnumSelect options={enumOptions(RolloverType)} value={cf.RolloverType} onChange={(v) => setCf({ RolloverType: v ?? 0 })} />
                </Field>
                <Field label="Rollover days"><NumberInput step="1" value={cf.RolloverDays} onChange={(v) => setCf({ RolloverDays: v ?? 0 })} /></Field>
                <Field label="Price adjustment">
                  <EnumSelect options={enumOptions(AdjustmentMode)} value={cf.AdjustmentMode} onChange={(v) => setCf({ AdjustmentMode: v ?? 0 })} />
                </Field>
                <div className="field wide">
                  <span>Contract months used</span>
                  <div className="toolbar">
                    {months.map((m) => {
                      const key = `Use${m}` as keyof ContinuousFuture;
                      return (
                        <label key={m} className="toolbar" style={{ gap: 4 }}>
                          <input type="checkbox" checked={Boolean(cf[key])} onChange={(e) => setCf({ [key]: e.target.checked } as Partial<ContinuousFuture>)} />
                          {m}
                        </label>
                      );
                    })}
                  </div>
                </div>
              </div>
            );
          })()}
        </>
      )}

      <ErrorBox error={error} />
    </Modal>
  );
}
