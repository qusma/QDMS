# QDMS — QUSMA Data Management System

Client/server system for acquiring, storing and distributing low-frequency historical and real-time market data (plus metadata,
dividends, earnings, economic releases). It's the data layer for QTS (`../QTS`, see its `CLAUDE.md`): QTS's backtester and trader
talk to QDMS through the `QDMSClient` NuGet package, pinned at **0.10.3**.

Open source (BSD-3, github.com/qusma/qdms). Dormant from Apr 2022 until Sep 2026, when it was modernized on the `modernize` branch:
.NET 10, EF Core + PostgreSQL, ASP.NET Core instead of Nancy, a headless host for Linux/Docker, and the WPF app and dead data
sources deleted. Known issues and plans are in `TODO.md`.

## Layout

| Project | Target | Role |
|---|---|---|
| `src/QDMS.Common` | netstandard2.1 | Shared model: `Instrument`, `OHLCBar`, `Datasource`, requests/DTOs, **source interfaces** (`Interfaces/`), `PriceAdjuster`, utils. **Assembly name is `QDMS`** (see gotchas) |
| `src/QDMSClient` | netstandard2.1 | Client library (NuGet `QDMSClient`): ZeroMQ for data, HTTP for metadata/jobs. Bundles `QDMS.dll` |
| `src/QDMS.Dal` | net10.0 | EF Core `MyDBContext` (metadata) + `DataDBContext` (bars); `Migrations/Metadata`, `Migrations/Data`; `Utils/DbSetup.cs` |
| `src/QDMS.Server` | net10.0 | ZMQ servers, HTTP endpoints (`Http/`), brokers, Quartz jobs, repositories, validators, `Utils/Seed.cs` |
| `src/QDMS.Host` | net10.0 web | **Composition root** (`Program.cs`): config, DI, migrations + seeding at startup, hosted services |
| `src/QDMS.DataSources/*` | net10.0 | IB, Binance, FRED, Tiingo |
| `src/QDMS.DataStorage/…Postgres` | net10.0 | `PostgresStorage`: bar storage (binary COPY + `ON CONFLICT` upserts) |
| `src/QDMS.Web` | React + TS | Web UI (Vite), built into `src/QDMS.Host/wwwroot/ui` and served at `/ui` (see Web UI below) |
| `tests/QDMS.Test` | net10.0 | NUnit 4 + Moq. HTTP and storage tests run against Postgres via **Testcontainers (needs Docker)** |
| `tools/QDMS.MigrateFromMySql` | net10.0 | One-off import from the old MySQL/MariaDB databases (`qdms`, `qdmsdata`, Quartz jobs) |
| `deploy/` | | `docker-compose.yml` (Postgres + QDMS) and `.env.example`; `Dockerfile` is at the repo root |

Build everything with `dotnet build QDMS.slnx`; test with `dotnet test QDMS.slnx`. Runs on Linux (and Windows). The web UI builds separately with npm
(see Web UI).

## Web UI

`src/QDMS.Web`: React 19 + TypeScript + Vite, AG Grid for tables, lightweight-charts for bars, TanStack Query for fetching. It replaces the old
WPF app's windows (no screens for the deleted data sources). The host serves it at `/ui` (`/` redirects there) without the API key; the page asks
for the key and sends it on every call like any other client.

- Build: `npm ci` then `npm run build` in `src/QDMS.Web` (writes `src/QDMS.Host/wwwroot/ui`, gitignored). `dotnet build` doesn't do this, so a
  fresh clone serves no UI until you build it. The `Dockerfile` has a Node stage for it.
- Dev: `npm run dev` (port 5173) proxies the API to a running QDMS on `http://localhost:5559` (override with `QDMS_URL`).
- It talks to the existing endpoints plus UI-only ones under `/api` (`QDMS.Server/Http/WebUiEndpoints.cs`: bars, fetch, storage info,
  currencies, time zones, log tail; `QDMS.Host/TiingoEndpoints.cs`: ticker search). QDMSClient doesn't use `/api`, so it may change freely.
- JSON comes with Newtonsoft's `$id`/`$ref` markers; `src/api.ts` resolves them. Session times travel as `OpeningAsSeconds`/`ClosingAsSeconds`
  (the `TimeSpan` properties are `[JsonIgnore]`d).
- `node_modules` has platform-specific binaries: install it on the OS you build on (Windows here), not from WSL.

## Architecture (see `architecture.md`)

Client → **Servers** (`QDMS.Server/Servers`: `HistoricalDataServer`, `RealTimeDataServer` over ZeroMQ, protobuf + LZ4; HTTP Minimal API endpoints in
`Http/` for instruments, exchanges, datasources, dividends, earnings, jobs, …) → **Brokers** (`QDMS.Server/Brokers`: historical, real-time,
continuous futures, dividends, earnings, economic releases) → **data sources** and/or **local storage**. Quartz schedules update jobs
(tables `qrtz_*` in the metadata DB, created by Quartz itself at startup).

- **Historical requests** carry a `DataLocation` (`LocalOnly`, `ExternalOnly`, `Both`). QTS almost always asks `LocalOnly`, so the store must
  already be filled by update jobs.
- **Each `Instrument` points at a `Datasource` row by name**, and the broker picks the `IHistoricalDataSource` whose `Name` matches. Renaming a
  source or moving instruments to a new one is a data migration, not just code.
- **Adjusted prices are computed by QDMS**, not taken from the source: sources fill raw OHLC plus `Dividend`/`Split` on bars, then
  `PriceAdjuster.AdjustData` builds `AdjOpen..AdjClose` (`PostgresStorage.AddData` triggers it when a batch has dividends or splits).
- **Databases:** by default metadata, bars and Quartz share one Postgres database (`ConnectionStrings:Qdms`); set `ConnectionStrings:QdmsData` to put
  bars elsewhere. Snake_case names; all `DateTime`s are `timestamp without time zone` (exchange-local, as before).

## Configuration

`src/QDMS.Host/appsettings.json`, overridable with environment variables (`Qdms__apiKey`, `ConnectionStrings__Qdms`, …):
- `ConnectionStrings:Qdms` (required), `ConnectionStrings:QdmsData` (optional)
- `Qdms:apiKey` (required; clients send it as the raw `Authorization` header), ports `httpPort` 5559, `hDBPort` 5555, `rtDBReqPort` 5556,
  `rtDBPubPort` 5557 (the same as the old QDMS; QDMSClient 0.10.3 expects them this way round)
- `Qdms:enableInteractiveBrokers` (default false) plus `ibClientHost`/`ibClientPort` (IB Gateway), `tiingoApiKey`, update-job email settings
- `appsettings.Development.json` sets `apiKey` to `123` for local runs (what QTS sends).

Deploying: see the comments in `deploy/docker-compose.yml`. The image must keep ICU and tzdata (exchanges store Windows time zone IDs).

## Adding a data source

1. Implement the interface from `QDMS.Common/Interfaces`: `IHistoricalDataSource` (event-based: `RequestHistoricalData` → raise
   `HistoricalDataArrived`), `IRealTimeDataSource`, `IDividendDataSource` / `IEarningsAnnouncementSource` / `IEconomicReleaseSource` (`Task`-based).
   `Tiingo.cs` is the model for an HTTP historical source (`HttpClient`, a request channel, always answers the request even on errors;
   tests with a fake `HttpMessageHandler` in `tests/QDMS.Test/DataSources/TiingoTest.cs`).
2. Register it in `ConfigureServices` in `src/QDMS.Host/Program.cs` as `IHistoricalDataSource` (etc.). Constructor dependencies such as `ISettings`
   come from DI.
3. Add a `Datasource` row in `Seed.SeedDatasources` (`QDMS.Server/Utils/Seed.cs`, runs on every start, adds missing names). Then point instruments at it.
4. Put API keys in `ISettings` (`QDMS.Common/Interfaces/ISettings.cs`) and `QdmsSettings` (`QDMS.Host/QdmsSettings.cs`), plus `appsettings.json`
   and `deploy/`.
5. Add the project reference to `QDMS.Host` (and to `QDMS.Test` for tests), and to the `Dockerfile` restore step.

## State of the data sources (Sep 2026)

| Source | Type | Status |
|---|---|---|
| Interactive Brokers | hist + RT | Historical verified against TWS 7496 (Sep 2026; indices incl. VIX, DAX/SMI on EUREX). RT not tested. Off unless `enableInteractiveBrokers`; needs TWS or IB Gateway |
| Binance | hist + RT | Ported (WebSocket4Net replaced by `ClientWebSocket`); not smoke-tested |
| FRED | hist | Ported; not smoke-tested (needs an API key) |
| Tiingo | hist (daily/weekly/monthly) | Working, verified end to end (Sep 2026). Raw prices + `divCash`/`splitFactor`, adjusted by `PriceAdjuster`. Disconnected unless `tiingoApiKey` is set |

Deleted as dead or unused: Yahoo, Quandl, BarChart, Bloomberg, ForexFeed, and the F# Nasdaq (dividends), CBOE (earnings), FXStreet (economic
releases) and Dividend.com sources. **So there are currently no dividend, earnings or economic-release sources**; those brokers can only serve
what's already in the database.

## Conventions and gotchas

- **Stay wire-compatible with QDMSClient 0.10.3.** HTTP routes, JSON shape (Newtonsoft, PascalCase, `PreserveReferencesHandling.Objects`, see
  `Http/HttpApi.cs`), query-string binding for GETs, error bodies, and ZMQ message formats must not change. Check any change against the
  published 0.10.3 package, not just the in-repo client.
- **Common's assembly name must stay `QDMS`**: the published client ships `QDMS.dll`, and MetaLinq-serialized predicates (`/instruments/predsearch`,
  economic release filters) carry `QDMS.Instrument, QDMS` type names. MetaLinq also can't serialize captured variables, only literals.
- The metadata unique index on instruments treats **NULLs as distinct** (as MySQL did), so instruments without exchange/expiry/strike can be
  duplicated. The imported 2021 data relies on this (see `TODO.md`, Data cleanup).
- **`DbContext`s aren't thread-safe**: HTTP endpoints get a scoped `IMyDbContext`; brokers and jobs create their own from
  `IDbContextFactory<MyDBContext>` (jobs via `Jobs/JobFactory.cs`).
- Migrations: `dotnet tool restore`, then `dotnet ef migrations add <Name> -p src/QDMS.Dal -c MyDBContext -o Migrations/Metadata` (or
  `DataDBContext`/`Migrations/Data`). The design-time factories in `QDMS.Dal/Utils/DesignTimeFactories.cs` don't need a running DB.
- **IB end dates** must be sent as `yyyyMMdd-HH:mm:ss` (UTC). Current TWS rejects the old `yyyyMMdd HH:mm:ss GMT` form (error 10314).
  The IB source logs all TWS messages itself, including status messages that belong to no request.
- **IB goes through `QDMS-IBClient` 2.0.0** (source: `../QDMS-IBClient`), which bundles IB's C# TWS API 10.50 verbatim. Sizes and volumes are
  `decimal` (`decimal.MaxValue` = not set, see `TWSUtils.ToVolume`), bar times are parsed by the wrapper (`HistoricalDataEventArgs.Time`), and
  **stock volumes come in shares**, no longer in lots of 100.
- Quartz is v4 (fully async, `ValueTask`). Job data is stored as JSON strings; the job classes are in `QDMS.Server.Jobs`, and a scheduled job
  stores that type name, so renaming job classes breaks existing schedules.
