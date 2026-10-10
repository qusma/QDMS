// Mirrors the server's models (QDMS.Common). Names are PascalCase as on the wire; enums are sent as numbers.
// Dates are strings without time zone ("2021-03-12T00:00:00", exchange-local), TimeSpans are "hh:mm:ss" (except session times).

export const InstrumentType = {
  Stock: 0, Option: 1, Future: 2, Index: 3, FutureOption: 4, Cash: 5, Bag: 6, Bond: 7, Warrant: 8, Commodity: 9, Bill: 10,
  CFD: 11, Undefined: 12, Backtest: 13, CryptoCurrency: 14, Fund: 15, Unknown: 16,
} as const;
export type InstrumentType = (typeof InstrumentType)[keyof typeof InstrumentType];

export const BarSize = {
  Tick: 0, OneSecond: 1, FiveSeconds: 2, FifteenSeconds: 3, ThirtySeconds: 4, OneMinute: 5, TwoMinutes: 6, FiveMinutes: 7,
  FifteenMinutes: 8, ThirtyMinutes: 9, OneHour: 10, OneDay: 11, OneWeek: 12, OneMonth: 13, OneQuarter: 14, OneYear: 15,
} as const;
export type BarSize = (typeof BarSize)[keyof typeof BarSize];

export const DayOfTheWeek = { Monday: 0, Tuesday: 1, Wednesday: 2, Thursday: 3, Friday: 4, Saturday: 5, Sunday: 6 } as const;
export type DayOfTheWeek = (typeof DayOfTheWeek)[keyof typeof DayOfTheWeek];

export const SessionsSource = { Exchange: 0, Template: 1, Custom: 2 } as const;
export type SessionsSource = (typeof SessionsSource)[keyof typeof SessionsSource];

export const OptionType = { Call: 0, Put: 1 } as const;
export type OptionType = (typeof OptionType)[keyof typeof OptionType];

export const DataLocation = { Both: 0, ExternalOnly: 1, LocalOnly: 2 } as const;
export type DataLocation = (typeof DataLocation)[keyof typeof DataLocation];

export const RolloverType = { Time: 0, Volume: 1, OpenInterest: 2, VolumeAndOpenInterest: 3, VolumeOrOpenInterest: 4 } as const;
export const AdjustmentMode = { NoAdjustment: 0, Ratio: 1, Difference: 2 } as const;
export const DayType = { Calendar: 0, Business: 1 } as const;
export const RelativeMonth = { PreviousMonth: -1, CurrentMonth: 0, NextMonth: 1 } as const;
export const WeekDayCount = { First: 0, Second: 1, Third: 2, Fourth: 3, Last: 4 } as const;
export const ReferenceDayType = { CalendarDays: 0, WeekDays: 1, LastDayOfMonth: 2 } as const;

/** name → value map of an enum object, as [label, value] pairs for selects */
export const enumOptions = (e: Record<string, number>) => Object.entries(e).map(([label, value]) => ({ label, value }));
export const enumName = (e: Record<string, number>, v: number | null | undefined) =>
  v === null || v === undefined ? '' : (Object.entries(e).find(([, x]) => x === v)?.[0] ?? String(v));

// Session times are sent as seconds since midnight (the TimeSpan properties are [JsonIgnore]d on the server)
export interface Session {
  ID: number;
  OpeningAsSeconds: number;
  ClosingAsSeconds: number;
  OpeningDay: DayOfTheWeek;
  ClosingDay: DayOfTheWeek;
  IsSessionEnd: boolean;
}
export interface ExchangeSession extends Session { ExchangeID: number }
export interface InstrumentSession extends Session { InstrumentID: number }
export interface TemplateSession extends Session { TemplateID: number }

export interface Exchange {
  ID: number;
  Name: string;
  LongName: string;
  Timezone: string;
  Sessions: ExchangeSession[];
}

export interface SessionTemplate {
  ID: number;
  Name: string;
  Sessions: TemplateSession[];
}

export interface Tag { ID: number; Name: string }
export interface Datasource { ID: number; Name: string }
export interface Currency { Id: number; Name: string; Code: string; NumericCode: number }

export interface ExpirationRule {
  DaysBefore: number;
  DayType: number;
  ReferenceRelativeMonth: number;
  ReferenceDayType: number;
  ReferenceDays: number;
  ReferenceWeekDayCount: number;
  ReferenceWeekDay: number;
  ReferenceDayMustBeBusinessDay: boolean;
}

export interface UnderlyingSymbol { ID: number; Symbol: string; Rule: ExpirationRule }

export interface ContinuousFuture {
  ID: number;
  InstrumentID: number;
  Instrument?: unknown;
  UnderlyingSymbolID: number;
  UnderlyingSymbol?: UnderlyingSymbol | null;
  Month: number;
  RolloverType: number;
  RolloverDays: number;
  AdjustmentMode: number;
  UseJan: boolean; UseFeb: boolean; UseMar: boolean; UseApr: boolean; UseMay: boolean; UseJun: boolean;
  UseJul: boolean; UseAug: boolean; UseSep: boolean; UseOct: boolean; UseNov: boolean; UseDec: boolean;
}

export interface Instrument {
  ID: number | null;
  Symbol: string;
  UnderlyingSymbol: string | null;
  Name: string;
  Type: InstrumentType;
  Multiplier: number | null;
  Expiration: string | null;
  OptionType: OptionType | null;
  Strike: number | null;
  Currency: string | null;
  MinTick: number | null;
  Industry: string | null;
  Category: string | null;
  Subcategory: string | null;
  IsContinuousFuture: boolean;
  ValidExchanges: string | null;
  DatasourceSymbol: string | null;
  TradingClass: string | null;
  ExchangeID: number | null;
  Exchange: Exchange | null;
  PrimaryExchangeID: number | null;
  PrimaryExchange: Exchange | null;
  DatasourceID: number | null;
  Datasource: Datasource | null;
  Tags: Tag[];
  SessionsSource: SessionsSource;
  SessionTemplateID: number | null;
  Sessions: InstrumentSession[];
  ContinuousFutureID: number | null;
  ContinuousFuture: ContinuousFuture | null;
}

export interface OHLCBar {
  DT: string;
  DTOpen: string | null;
  Open: number;
  High: number;
  Low: number;
  Close: number;
  AdjOpen: number | null;
  AdjHigh: number | null;
  AdjLow: number | null;
  AdjClose: number | null;
  Volume: number | null;
  OpenInterest: number | null;
  Dividend: number | null;
  Split: number | null;
  InstrumentID?: number;
  Frequency?: BarSize;
}

export interface StoredDataInfo {
  InstrumentID: number;
  Frequency: BarSize;
  EarliestDate: string;
  LatestDate: string;
}

export interface DataSourceStatus {
  Name: string;
  RealtimeConnected: boolean | null;
  HistoricalConnected: boolean | null;
  EconReleasesConnected: boolean | null;
}

export interface DataUpdateJobSettings {
  ID?: number;
  Name: string;
  UseTag: boolean;
  InstrumentID: number | null;
  Instrument: Instrument | null;
  TagID: number | null;
  Tag: Tag | null;
  WeekDaysOnly: boolean;
  Time: string;
  Frequency: BarSize;
}

export interface FetchRequest {
  Frequency: BarSize;
  StartingDate: string;
  EndingDate: string;
  DataLocation: DataLocation;
  SaveToStorage: boolean;
  RTHOnly: boolean;
  TimeoutSeconds?: number;
}

export interface FetchResult { Bars: OHLCBar[]; Errors: string[] }
export interface TimeZone { Id: string; DisplayName: string }
export interface LogTail { File: string | null; Lines: string[] }

export interface TiingoSearchResult {
  Ticker: string;
  Name: string;
  AssetType: string;
  IsActive: boolean;
  CountryCode: string;
  PermaTicker: string;
}

export interface TiingoMeta {
  Ticker: string;
  Name: string;
  ExchangeCode: string;
  StartDate: string | null;
  EndDate: string | null;
  Description: string | null;
}
