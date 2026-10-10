// HTTP client for the QDMS API.
// The server's JSON comes from Newtonsoft with PreserveReferencesHandling.Objects: every object has a "$id",
// and repeated objects are sent as {"$ref": "<id>"}. resolveRefs turns that back into plain objects.

import type {
  Currency, Datasource, DataSourceStatus, DataUpdateJobSettings, Exchange, FetchRequest, FetchResult, Instrument, LogTail, OHLCBar,
  SessionTemplate, StoredDataInfo, Tag, TiingoMeta, TiingoSearchResult, TimeZone, UnderlyingSymbol, BarSize,
} from './types';

const KEY_STORAGE = 'qdms.apiKey';

export function getApiKey(): string {
  try {
    return localStorage.getItem(KEY_STORAGE) ?? '';
  } catch {
    return '';
  }
}

export function setApiKey(key: string) {
  try {
    localStorage.setItem(KEY_STORAGE, key);
  } catch {
    // private window etc.: the key just won't be remembered
  }
}

export class ApiError extends Error {
  status: number;
  details: string[];

  constructor(status: number, message: string, details: string[] = []) {
    super(message);
    this.status = status;
    this.details = details;
  }
}

/** Replace {"$ref": id} with the object that carries that "$id", and drop the $id markers. */
export function resolveRefs<T>(data: unknown): T {
  const byId = new Map<string, Record<string, unknown>>();

  const collect = (v: unknown) => {
    if (Array.isArray(v)) v.forEach(collect);
    else if (v && typeof v === 'object') {
      const o = v as Record<string, unknown>;
      if (typeof o.$id === 'string') byId.set(o.$id, o);
      Object.values(o).forEach(collect);
    }
  };
  collect(data);

  const seen = new Set<unknown>();
  const replace = (v: unknown): unknown => {
    if (Array.isArray(v)) {
      for (let i = 0; i < v.length; i++) v[i] = replace(v[i]);
      return v;
    }
    if (v && typeof v === 'object') {
      const o = v as Record<string, unknown>;
      if (typeof o.$ref === 'string') return byId.get(o.$ref) ?? null;
      if (seen.has(o)) return o;
      seen.add(o);
      delete o.$id;
      for (const k of Object.keys(o)) o[k] = replace(o[k]);
      return o;
    }
    return v;
  };
  return replace(data) as T;
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  const res = await fetch(path, {
    method,
    headers: {
      Authorization: getApiKey(),
      ...(body !== undefined ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body !== undefined ? JSON.stringify(body, jsonReplacer) : undefined,
  });

  const text = await res.text();
  let json: unknown = undefined;
  if (text) {
    try {
      json = JSON.parse(text);
    } catch {
      json = undefined;
    }
  }

  if (!res.ok) {
    const j = (json ?? {}) as { Message?: string; Details?: string; ErrorMessages?: string[] };
    if (j.ErrorMessages) throw new ApiError(res.status, 'Validation failed', j.ErrorMessages);
    throw new ApiError(res.status, j.Message || `${res.status} ${res.statusText}`, j.Details ? [j.Details] : []);
  }

  return resolveRefs<T>(json);
}

// Objects we send back come from the server with their navigation properties filled in.
// Cyclic ones (Exchange.Sessions[].Exchange, ...) are cut here.
function jsonReplacer(this: unknown, key: string, value: unknown) {
  if (key === 'Exchange' && this && typeof this === 'object' && 'ExchangeID' in this && 'OpeningAsSeconds' in this) return undefined;
  if ((key === 'Instrument' || key === 'Template') && this && typeof this === 'object' && 'OpeningAsSeconds' in this) return undefined;
  return value;
}

const get = <T>(path: string) => request<T>('GET', path);
const post = <T>(path: string, body?: unknown) => request<T>('POST', path, body ?? {});
const put = <T>(path: string, body: unknown) => request<T>('PUT', path, body);
const del = <T>(path: string, body?: unknown) => request<T>('DELETE', path, body);

const qs = (params: Record<string, string | number | boolean | undefined | null>) =>
  new URLSearchParams(Object.entries(params).filter(([, v]) => v !== undefined && v !== null && v !== '')
    .map(([k, v]) => [k, String(v)])).toString();

export const api = {
  // checks the key
  ping: () => get<Datasource[]>('/datasources'),

  instruments: () => get<Instrument[]>('/instruments'),
  instrument: (id: number) => get<Instrument>(`/instruments/${id}`),
  addInstrument: (i: Instrument) => post<Instrument>('/instruments', stripInstrument(i)),
  updateInstrument: (i: Instrument) => put<Instrument>('/instruments', stripInstrument(i)),
  deleteInstrument: (id: number) => del<Instrument>(`/instruments/${id}`),

  storageInfo: () => get<StoredDataInfo[]>('/api/storageinfo'),
  bars: (id: number, frequency: BarSize, from?: string, to?: string) =>
    get<OHLCBar[]>(`/api/instruments/${id}/bars?${qs({ Frequency: frequency, From: from, To: to })}`),
  updateBars: (id: number, frequency: BarSize, bars: OHLCBar[], adjust: boolean) =>
    put<{ Count: number }>(`/api/instruments/${id}/bars?${qs({ Frequency: frequency, Adjust: adjust })}`, bars),
  addBars: (id: number, frequency: BarSize, bars: OHLCBar[], overwrite: boolean, adjust: boolean) =>
    post<{ Count: number }>(`/api/instruments/${id}/bars?${qs({ Frequency: frequency, Overwrite: overwrite, Adjust: adjust })}`, bars),
  deleteBars: (id: number, frequency: BarSize, bars: OHLCBar[]) =>
    post<{ Count: number }>(`/api/instruments/${id}/bars/delete?${qs({ Frequency: frequency })}`, bars),
  deleteAllBars: (id: number, frequency: BarSize) =>
    post<{ Count: number }>(`/api/instruments/${id}/bars/delete?${qs({ Frequency: frequency, All: true })}`),
  fetch: (id: number, req: FetchRequest) => post<FetchResult>(`/api/instruments/${id}/fetch`, req),

  datasources: () => get<Datasource[]>('/datasources'),
  datasourceStatus: () => get<DataSourceStatus[]>('/datasources/status'),
  activeStreams: () => get<unknown[]>('/datasources/activestreams'),

  exchanges: () => get<Exchange[]>('/exchanges'),
  addExchange: (e: Exchange) => post<Exchange>('/exchanges', e),
  updateExchange: (e: Exchange) => put<Exchange>('/exchanges', e),
  deleteExchange: (id: number) => del<Exchange>(`/exchanges/${id}`),

  sessionTemplates: () => get<SessionTemplate[]>('/sessiontemplates'),
  addSessionTemplate: (t: SessionTemplate) => post<SessionTemplate>('/sessiontemplates', t),
  updateSessionTemplate: (t: SessionTemplate) => put<SessionTemplate>('/sessiontemplates', t),
  deleteSessionTemplate: (id: number) => del<SessionTemplate>(`/sessiontemplates/${id}`),

  tags: () => get<Tag[]>('/tags'),
  addTag: (t: Tag) => post<Tag>('/tags', t),
  updateTag: (t: Tag) => put<Tag>('/tags', t),
  deleteTag: (id: number) => del<Tag>(`/tags/${id}`),

  underlyingSymbols: () => get<UnderlyingSymbol[]>('/underlyingsymbols'),
  addUnderlyingSymbol: (u: UnderlyingSymbol) => post<UnderlyingSymbol>('/underlyingsymbols', u),
  updateUnderlyingSymbol: (u: UnderlyingSymbol) => put<UnderlyingSymbol>('/underlyingsymbols', u),
  deleteUnderlyingSymbol: (id: number) => del<UnderlyingSymbol>(`/underlyingsymbols/${id}`),

  dataUpdateJobs: () => get<DataUpdateJobSettings[]>('/jobs/dataupdatejobs'),
  addDataUpdateJob: (j: DataUpdateJobSettings) => post<DataUpdateJobSettings>('/jobs/dataupdatejobs', stripJob(j)),
  deleteDataUpdateJob: (j: DataUpdateJobSettings) => del<DataUpdateJobSettings>('/jobs/dataupdatejobs', stripJob(j)),
  runJob: (type: string, name: string) => post<void>(`/jobs/${encodeURIComponent(type)}/${encodeURIComponent(name)}/run`),

  currencies: () => get<Currency[]>('/api/currencies'),
  timezones: () => get<TimeZone[]>('/api/timezones'),
  logs: (lines: number) => get<LogTail>(`/api/logs?lines=${lines}`),

  tiingoSearch: (query: string) => get<TiingoSearchResult[]>(`/api/tiingo/search?${qs({ query })}`),
  tiingoMeta: (ticker: string) => get<TiingoMeta>(`/api/tiingo/meta/${encodeURIComponent(ticker)}`),
};

// The server attaches Exchange/Datasource/Tags by ID; their nested collections aren't needed and only bloat the request.
function stripInstrument(i: Instrument): Instrument {
  const lite = <T extends { Sessions?: unknown }>(x: T | null | undefined) => (x ? { ...x, Sessions: undefined } : x);
  return {
    ...i,
    Exchange: lite(i.Exchange) ?? null,
    PrimaryExchange: lite(i.PrimaryExchange) ?? null,
    ContinuousFuture: i.ContinuousFuture ? { ...i.ContinuousFuture, Instrument: undefined } : null,
  } as Instrument;
}

function stripJob(j: DataUpdateJobSettings): DataUpdateJobSettings {
  return { ...j, Instrument: j.Instrument ? stripInstrument(j.Instrument) : null };
}
