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
- [x] **Web UI** (Sep 2026): `src/QDMS.Web` (React + TypeScript + Vite), served by the host at `/ui`. Instruments (add manually or from
      Tiingo, edit, clone, delete, tags, update data), bar viewer/editor with chart, CSV import/export, data update jobs with run-now,
      exchanges, session templates, underlying symbols, tags, data source status and the log. Checked in Edge (light/dark, phone width) against a
      copy of the database. Remaining gaps:
      - [ ] "Add instrument" from IB (contract search), Binance and FRED; only Tiingo and manual entry exist.
      - [ ] Dividend, earnings and economic release jobs and browsers: skipped, there are no sources for them (see step 2).
      - [ ] No automated UI tests; the check was a one-off Playwright script.
      - [ ] The API key sits in the browser's localStorage and travels over plain HTTP, same as for the other clients (see the deploy note).
- [ ] **CI**: AppVeyor config was deleted; add a GitHub Actions workflow (`dotnet test` works on ubuntu runners, which have Docker).

## P0 — step 2: get daily data flowing again

- [x] **Tiingo historical source** (Sep 2026): daily/weekly/monthly bars from raw prices + `divCash`/`splitFactor`, adjusted by QDMS's own
      `PriceAdjuster` (Tiingo's adj fields are ignored). Registered in `Program.cs`; stays disconnected without `tiingoApiKey`. Verified end to
      end with SPY through the 0.10.3 client (fetch, dividend adjustment, incremental update).
- [ ] **Tiingo rate limits**: requests go out one at a time with no throttling. The free tier has an hourly request cap (check the account's
      plan page), so a single update job over ~150 instruments may hit it; failed instruments are logged as errors and simply retried on the next
      run. Either spread the jobs out (several tags at different times), add a throttle in `Tiingo.cs`, or buy a paid plan.
- [x] **Tiingo instruments re-pointed and backfilled** (Oct 2026): AAPLn 93, QQQn 1598, TLTn 146, SPY 20, DXJ 101, EEM 189, EFA 107, EWA 111,
      EWG 116, EWH 117, EWI 118, EWL 120, FXI 130, GDX 131, GLD 132, THD 145, UNG 596, VGK 147 now point at Tiingo and hold a curated daily
      history to 2026-10-09: Tiingo as the base, with the old Yahoo bars (and Yahoo as of Oct 2026 as a third opinion) used where Tiingo was
      wrong. A plain Tiingo refetch would **not** reproduce it, so don't delete and refetch these. Everything is in
      `backups/tiingo-backfill-2026-10/` (gitignored): the bars before the change, the raw downloads, the curated files, a decision log per
      instrument and the scripts (`merge.py` has the rules). The old bars are also in the table `bars_backup_yahoo_20261010`. What Tiingo gets wrong:
      - Bad prints in 1996 for EWI/EWL (~90 days each) and scattered closes elsewhere; a duplicated bar on 2003-01-29 for several tickers;
        pre-2001 fractional prices rounded to cents; split factors like 7.000007.
      - Dividends: AAPL's first two (1987) missing; GDX 2007 doubled and 2011 wrong; VGK Dec 2020 on 12-18 instead of 12-21.
      - Yahoo is no better on dividends: it has spurious copies of earlier payments (2003-12-16 for five iShares funds, EWI/EWL Dec 2000, EWL Dec 1998,
        two for QQQ), so Yahoo-only dividends were not added.
      - Unresolved, Yahoo kept: opens on 50-200 days per fund (mostly 2006-08), GDX/UNG closes in 2008. EWG 2020-12-14 dividend: Tiingo's 0.249 kept (Yahoo 0.387).
      - Not tested yet: an incremental Tiingo update on top of the curated history (bars were current when loaded).
- [x] **Index instruments re-pointed to IB and backfilled** (Oct 2026): SPX 105, NDX 152, HSI 169, XJO 172, DAX 170, SMI 155 moved from Yahoo to
      Interactive Brokers (`datasource_symbol` cleared; DAX and SMI on EUREX, since IB no longer knows `DTB`/`SOFFEX`); VIX 102 and VIX3M 103 were
      on IB already. All eight now run to 2026-10-09. Backups: tables `bars_backup_indices_20261010` and `instruments_backup_20261010`; downloads,
      curated files, decision logs and scripts in `backups/tiingo-backfill-2026-10/indices/` (`idxmerge.py` has the rules).
      - The six equity indices: stored bars before 2020 untouched; from 2020-01-02 IB is the base, out-voted where the stored bar and Yahoo
        (Oct 2026) agree against it. IB sends flat filler bars (O=H=L=C) on market closures (HSI typhoon days, holidays): dropped, or replaced
        from Yahoo where Yahoo has a real bar (NDX and VIX3M 2021-01-28). The last stored bar of an instrument can be a partial day.
      - VIX and VIX3M: closes corrected to Cboe's official history over the whole series (VIX: 44 stored closes, mostly 2012-14 and early-close
        days, and 29 of IB's after 2021; VIX3M: 7). Cboe's open/high/low have bad ticks in older years (and 47 early opens outside the day's
        range), so stored O/H/L were kept and Cboe's used only after Jan 2021 where Yahoo confirmed them. **IB's VIX close is wrong on a few
        days a year**, so daily IB updates will bring those back: re-check against `https://cdn.cboe.com/api/global/us_indices/daily_prices/VIX_History.csv`
        now and then, or write a small Cboe source.
      - NDX's stored split of 2.0 on 1994-01-03 is deliberate (the index level was halved), not a data error.
      - **Never point an index at Tiingo**: e.g. `spx` there is an unrelated ASX stock (Spenda Ltd), so it would silently load the wrong data.
      - Other Yahoo instruments stay as they are (history frozen at Mar 2021, fine for research).
      - Open: bars saved from IB have no adjusted prices (`adj_*` null, as VIX had for 2017-21), while the backfilled index bars have
        adj = raw. Check what QTS reads for indices before relying on daily IB updates.
- [ ] **Publish QDMS-IBClient 2.0.1** (fix is in `../QDMS-IBClient`, uncommitted): current TWS (API server version 226) closes the connection
      if any request arrives before it has finished accepting the client ("Ignoring API request ... since API is not accepted" in the TWS log),
      and `Client.Connect` sent `reqIds` right after `StartApi`. It now waits for TWS's own next-valid-id message. Until 2.0.1 is on nuget.org
      and referenced here, **IB only connects with the patched DLL copied by hand** into `src/QDMS.Host/bin/Debug/net10.0/` (the 2.0.0 one is
      kept there as `.nuget-2.0.0.bak`); any `dotnet build` puts the broken one back.
- [x] **Two IB source bugs fixed** (Oct 2026, uncommitted): `TWSUtils` set `SecIdType = "None"`, which TWS now rejects (error 321 "Unknown
      security type : None"); and `IB.Historical` gave the last sub-request of a split request the id that the next request then reused, so
      only the first multi-year request after startup returned its data.
- [x] **Update jobs** (Oct 2026): `DailyTiingo` (tag `DailyTiingoUpdate`: the 38 funds FullPfolio2/3 use) at 02:30 and `DailyIB` (tag
      `DailyIBUpdate`: the 8 indices they use, plus VXN and VVIX) at 23:00, every day, server local time (Europe: after the US close and before
      Sydney opens, so no partial bars; on a UTC server 23:00 would fall inside the ASX session, so move it). IB only works while TWS/Gateway is
      running. Three bugs fixed on 2026-10-15 (`84c1ebd`): the job disposed the shared storage, a 5 s timeout, "run now" broken. VXMT and the two
      VIX continuous futures were taken out of the tag (dead contracts, errors 162/200 every run).
- [ ] **IB message 2188 is logged as an error** ("Up-to-the-second historical data requires additional subscription for the API"): it is
      informational, the daily bars still arrive and are saved. But the update job seems to count it as the end of the request: `DailyIB`
      logged "completed" before its data arrived. Treat 2188 as a warning in the IB source and don't end the request on it.
- [ ] **Running QDMS on the desktop with TWS**: TWS API settings: socket clients enabled, port 7496, "localhost only" on, Read-Only API on while
      QTSTrader2 isn't trading (it's global, so untick it for live trading). QDMS must run natively on Windows (WSL's NAT address can't reach a
      localhost-only TWS): `Qdms__enableInteractiveBrokers=true`, `Qdms__ibClientPort=7496` (default 4002 is the Gateway paper port). Client IDs
      99/98 don't clash with QTSTrader2's 0. Postgres can stay in WSL's Docker (published ports show up on Windows localhost).
- [ ] **Dividends and earnings**: there are no sources any more (the Nasdaq dividends and CBOE/Zacks earnings scrapers were deleted as dead).
      Options: Tiingo's per-bar `divCash` (now stored on the bars as `Dividend`) may cover dividends for QTS's needs; they'd also need writing to
      the `dividends` table, which is what QTS's `DividendRequest` reads;
      earnings needs a new source (EODHD calendar) or a decision to drop QTS's earnings-based signals.
- [x] **Port agreement with QTS** (Sep 2026): QTS's backtester and CLI moved from HTTP port 5551 to **5559**. The modernized host also had
      the real-time ports swapped (`rtDBPubPort`/`rtDBReqPort`); back to the old 5557/5556, which QDMSClient 0.10.3 expects. The local
      Development API key is `123`, what QTS sends.

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
