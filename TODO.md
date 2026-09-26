# TODO — QDMS

Known issues and plans, written Sep 2026 while reviving QTS. Priorities: **P0** blocks getting daily data flowing to QTS again; **P1** correctness
and robustness; **P2** cleanup; **P3** nice to have.

## Context: replacing Yahoo

QTS needs, per its workspaces:
- adjusted daily bars for ~150 instruments: US ETFs, including thin country ETFs (EIS, EIRL, THD), single stocks, and indices such as ^VIX and VIX3M
- dividends and earnings dates
- index history back to the 1960s: QTS's `DOTMRank` references Nov 1968

Yahoo was the bar source, and yfinance-style access is now routinely rate-limited or blocked (Yahoo never had an official API).

Options (prices as of Sep 2026; check before buying):

| Option | Cost | Good for | Gaps |
|---|---|---|---|
| **Tiingo** | Free tier: 500 symbols/month, 30+ years; paid flat-rate from ~$30/mo | Clean US EOD data, well adjusted. Per-bar `divCash` and `splitFactor` fit `PriceAdjuster` directly. A skeleton project already exists. | History from ~1990s; no VIX indices |
| **EODHD** | ~$20/mo EOD all exchanges; ~$60 with fundamentals | Breadth per dollar; dividends and splits; earnings calendar on higher tiers | Adjustment quality reputedly below Tiingo |
| **Norgate Data** | Paid (see site); 21-day trial | Survivorship-bias-free, US back to 1950, index constituents | Windows-local database, not a web API; overkill unless researching stock universes |
| **Interactive Brokers** | Free with account (plus market-data subscriptions) | Already implemented here; adjusted daily bars | Pacing limits; depth varies; no earnings |
| **Cboe CSVs** | Free | Official VIX / VIX3M history | Manual download or a small scraper |
| **Stooq CSVs** | Free | Long index history for a one-off backfill | Unofficial; no API guarantees |

**Plan (updated Sep 2026):** Tiingo for ETFs and stocks; IB for all indices, including VIX/VIX3M (Tiingo has no indices at all). The
imported Yahoo history already reaches back far enough (SPX from 1950), so no pre-1990 backfill is needed. EODHD's fundamentals tier only if a
structured earnings source is needed.

Sources:
- [Tiingo EOD](https://www.tiingo.com/products/end-of-day-stock-price-data)
- [Tiingo on EOD APIs](https://www.tiingo.com/blog/best-stock-price-api/)
- [EODHD](https://eodhd.com/financial-apis/api-for-historical-data-and-volumes)
- [API comparison 2026](https://www.nb-data.com/p/best-financial-data-apis-in-2026)
- [yfinance blocking](https://medium.com/@trading.dude/why-yfinance-keeps-getting-blocked-and-what-to-use-instead-92d84bb2cc01)
- [yfinance #2480](https://github.com/ranaroussi/yfinance/issues/2480)
- [Norgate](https://norgatedata.com/)
- [Norgate review](https://alvarezquanttrading.com/blog/norgate-data-review/)

## P0 — step 1: headless Linux service (mostly done, Sep 2026, branch `modernize`)

Done: .NET 10, EF Core 10 + PostgreSQL, Nancy → ASP.NET Core Minimal APIs, Quartz 4, `QDMS.Host` as the composition root, Docker
Compose deployment (`deploy/`), WPF app and dead sources deleted, tests ported (Testcontainers), MySQL import tool
(`tools/QDMS.MigrateFromMySql`). Wire-compatible with QDMSClient 0.10.3, verified with the published package over HTTP and ZMQ.

- [ ] **Deploy to the Linux server**: `deploy/docker-compose.yml`, then run `tools/QDMS.MigrateFromMySql` against it (through an SSH tunnel to
      Postgres) with the 2021 dump loaded into a temporary MariaDB. Keep the ZMQ/HTTP ports on the LAN or a VPN (e.g. Tailscale): the API key is
      the only protection and it travels in plain HTTP.
- [ ] **Commit and merge** the `modernize` branch.
- [x] **QDMS-IBClient 2.0.0** published to nuget.org (Sep 2026; source in `../QDMS-IBClient`). Bundles IB's TWS API 10.50 (was 9.7x).
- [ ] **Publish QDMSClient 0.11.0** (optional): targets netstandard2.1 now; QTS can stay on 0.10.3.
- [ ] **IB on the server**: for now QDMS runs on the Windows desktop and uses the TWS there (see step 2). Moving it to the server needs an IB
      Gateway (headless via IBC), and IB allows one session per username, so a Gateway with the trading login kicks out the desktop TWS. Options:
      a second username (market data subscriptions are per user), connect to the desktop TWS over the network (untick "localhost only", trust
      the server IP), or a paper login sharing live market data (unverified whether that works while live is logged in).
- [ ] **Web UI** (after the Tiingo work below; it doesn't block data flow). Serve it as a static SPA from the host, on top of the HTTP API.
      Minimum screens, taken from what the old WPF windows did:
      - instruments list and add/edit/clone/delete
      - datasources, exchanges, sessions, tags
      - scheduled update jobs, with a run-now button (`POST /jobs/{type}/{name}/run` exists)
      - a bar viewer/editor for one instrument
      - CSV data import (needed for the VIX / pre-1990 backfills)
      - log view
      Until it exists, do admin work through the HTTP API, SQL, or small CLI commands on the host.
- [ ] **CI**: AppVeyor config was deleted; add a GitHub Actions workflow (`dotnet test` works on ubuntu runners, which have Docker).

## P0 — step 2: get daily data flowing again

- [x] **Tiingo historical source** (Sep 2026): daily/weekly/monthly bars from raw prices + `divCash`/`splitFactor`, adjusted by QDMS's own
      `PriceAdjuster` (Tiingo's adj fields are ignored). Registered in `Program.cs`; stays disconnected without `tiingoApiKey`. Verified end to
      end with SPY through the 0.10.3 client (fetch, dividend adjustment, incremental update).
- [ ] **Tiingo rate limits**: requests go out one at a time with no throttling. The free tier has an hourly request cap (check the account's
      plan page), so a single update job over ~150 instruments may hit it; failed instruments are logged as errors and simply retried on the next
      run. Either spread the jobs out (several tags at different times), add a throttle in `Tiingo.cs`, or buy a paid plan.
- [ ] **Re-point the production instruments.** QTS's production workspace (`FullPfolio2`) needs daily bars for these instrument IDs only
      (the other IB IDs it references have no bars; they're for order execution):
      - **Tiingo** (from Yahoo): AAPLn 93, QQQn 1598, TLTn 146 (these three use `datasource_symbol`), SPY 20, DXJ 101, EEM 189, EFA 107,
        EWA 111, EWG 116, EWH 117, EWI 118, EWL 120, FXI 130, GDX 131, GLD 132, THD 145, UNG 596, VGK 147. Refetch full history (one request
        each), so all adjusted prices come from one source.
      - **IB** (from Yahoo): SPX 105, NDX 152, HSI 169, XJO 172 as they are; DAX 170 and SMI 155 also need `exchange_id` → EUREX (102), since IB
        no longer knows `DTB`/`SOFFEX`. Clear `datasource_symbol` (the Yahoo `^GSPC`-style symbols). Keep the Yahoo history: IB closes match it
        on overlapping dates.
      - **IB already**: VIX 102, VIX3M 103.
      All of this was verified on a copy of the import in Sep 2026 (every index fetched through QDMS from TWS). **Never point an index at Tiingo**:
      e.g. `spx` there is an unrelated ASX stock (Spenda Ltd), so it would silently load the wrong data.
      Other Yahoo instruments stay as they are (history frozen at Mar 2021, fine for research).
- [ ] **Update jobs**: one daily job for the Tiingo group, one for the IB group (IB only works while TWS is running on the desktop).
- [ ] **Running QDMS on the desktop with TWS**: TWS API settings: socket clients enabled, port 7496, "localhost only" on, Read-Only API on while
      QTSTrader2 isn't trading (it's global, so untick it for live trading). QDMS must run natively on Windows (WSL's NAT address can't reach a
      localhost-only TWS): `Qdms__enableInteractiveBrokers=true`, `Qdms__ibClientPort=7496` (default 4002 is the Gateway paper port). Client IDs
      99/98 don't clash with QTSTrader2's 0. Postgres can stay in WSL's Docker (published ports show up on Windows localhost).
- [ ] **Dividends and earnings**: there are no sources any more (the Nasdaq dividends and CBOE/Zacks earnings scrapers were deleted as dead).
      Options: Tiingo's per-bar `divCash` (now stored on the bars as `Dividend`) may cover dividends for QTS's needs; they'd also need writing to
      the `dividends` table, which is what QTS's `DividendRequest` reads;
      earnings needs a new source (EODHD calendar) or a decision to drop QTS's earnings-based signals.
- [ ] **Port agreement with QTS**: the new host's `httpPort` defaults to **5559**. QTS's `CLIBacktesterCore` still uses 5551 (see QTS `TODO.md`).

## Data cleanup (found importing the Mar 2021 dump, `../127_0_0_1.sql`)

- [ ] **Duplicate instruments**: RUT (Yahoo) IDs 168 and 474, both with ~8,440 bars; VIX (IB) IDs 17 (no bars) and 102. Check which IDs QTS
      workspaces reference, then delete the others. (The unique index treats NULLs as distinct, as MySQL did, which is how these got in.)
- [ ] **Dead datasources still referenced**: Yahoo (322 instruments, 225 with daily bars), Google (2), Quandl (3), BarChart (1). Re-point the
      ones that matter to Tiingo (see the Tiingo item below).
- [ ] **Old update jobs** (recovered to `jobs.json` by the migration tool, not rescheduled): the Yahoo tag job, two IB jobs, and dividend/earnings
      jobs using the dead Nasdaq and CBOE sources. Recreate the useful ones against Tiingo/IB.
- [ ] **Bad earnings rows**: some `earnings_announcements` have date 0001-01-01.
- [ ] `indexdata` in the dump has VIX futures (`vix_futures`, `vix_futures_cont`, ~35k rows) that were never in QDMS. Import if the VIX strategies need them.

## P1 — correctness and robustness

- [ ] **Smoke-test the remaining sources** (IB real-time, Binance, FRED) and record the result in the table in `AGENTS.md`. IB historical and
      Tiingo were verified in Sep 2026.
- [ ] **IB errors can leave requests hanging** (from reading `IB.cs`, not observed yet): only codes 162/200/420/10225 are handled; any other
      error on a historical request (e.g. 10314, bad date format) is logged and raised but never completes the request, so it waits until the
      update job's timeout.
- [ ] **IB real-time client didn't connect after a quick restart** (Sep 2026): "Unable to read beyond the end of the stream", probably TWS
      still holding client ID 98 from the killed process. Check whether the broker's reconnect timer recovers.
- [ ] **`ContinuousFuturesBroker.cs:658`**: doesn't check that the data series actually cover the current date.
- [ ] **`RealTimeDataBroker`**: real-time ticks aren't saved to storage (there's no tick storage since TeaFiles was deleted).
- [ ] **`QDMSClient.RealtimeData.cs:194`**: "solve issue with null request".
- [ ] **`TWSUtils.cs:233`**: contract month format for options/futures may be `yyyyMM`.
- [ ] **`MyUtils.cs:391`**: untested utility code ("write a test for this stuff").
- [ ] **Instrument uniqueness**: the unique index treats NULLs as distinct, so stocks (no exchange/expiry/strike) can be duplicated. Once the
      data cleanup below is done, consider `NULLS NOT DISTINCT` (Postgres 15+) via a migration.
- [ ] **Dividend/earnings/economic release saves** go through `Upsert` one row at a time, each a query (`DividendsBroker.cs:114`,
      `EarningsAnnouncementBroker.cs:112`, `EconomicReleaseBroker.cs:98`). Fine for daily updates, slow for big imports.

## P2 — cleanup

- [ ] **`RealTimeDataServer.cs:256`**: move validation to a separate validator.
- [ ] **FRED** uses the obsolete `WebClient` (`FRED.cs:82`, `FredUtils.cs:75`); move to `HttpClient`.
- [ ] **Build warnings** (9 on a clean build): unused events on Binance, IB and `RealTimeFromHistoricalDataSource`, plus FRED's `WebClient`.
- [ ] **Blocking `.Result` calls** left from the pre-async design: the jobs (`DataUpdateJob.cs:115`, `DividendUpdateJob.cs:68,100`,
      `EarningsUpdateJob.cs:68,99`, `EconomicReleaseUpdateJob.cs:77`; Quartz 4 jobs are async now, so these can just `await`) and
      `ContinuousFuturesBroker.cs:268,941`.

## P3 — nice to have

- [ ] **Tiingo extras**: IEX intraday endpoint, crypto/FX endpoints, real-time via their websocket (`Tiingo.cs` is historical EOD only).
- [ ] **Binance extras**: historical tick data via aggTrades (`Binance.cs:91`); split the client into its own package (`:21`).
- [ ] **Seed data** for exchange sessions (`Seed.cs:1577`).
