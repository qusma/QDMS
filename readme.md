QUSMA Data Management System (QDMS)
===================================

QDMS is a client/server system for acquiring, storing, managing, and distributing low-frequency historical and real-time data, written in C#. 

The server acts as a broker between clients and external data sources, as well as a local database of historical data. It runs headless (Linux or Windows, Docker image included) and is managed through its HTTP API.

A client library is provided which can access the server either locally or over a network, to request data/metadata, schedule jobs, etc. Get the client library on NuGet: [![Nuget](https://img.shields.io/nuget/v/QDMSClient)](https://www.nuget.org/packages/QDMSClient/)

For bug reports, feature requests, etc. use either the GitHub issue tracker or [gitter chat](https://gitter.im/qusma/community).


Features
--------
* Manage metadata on stocks, options, futures, CFDs, cryptocurrencies, etc.
* Stream real time OHLC & tick data to your applications.
* Download historical and real time data from external data sources and store it locally.
* Download and store earnings announcements, dividends, economic releases.
* Generate continuous futures data.
* Schedule automatic data updates.


Supported Data Sources
----------------------

| Data Source                           | Historical Data supported | Real Time Data supported |
|-------------------------------------- | ------------------------- | ------------------------ |
| Interactive Brokers                   | :white_check_mark:        | :white_check_mark:       |
| Binance                               | :white_check_mark:        | :white_check_mark:       |
| FRED (Federal Reserve Economic Data)  | :white_check_mark:        |                          |
| Tiingo (end of day)                   | :white_check_mark:        |                          |

Adding more sources is easy and contributions are welcome.


Running
-------
Requirements: .NET 10 and PostgreSQL, or just Docker.

```
cd deploy
cp .env.example .env    # set POSTGRES_PASSWORD and QDMS_API_KEY
docker compose up -d --build
```

Or run `src/QDMS.Host` directly (`dotnet run --project src/QDMS.Host`), configured through `appsettings.json` or environment variables
(`ConnectionStrings__Qdms`, `Qdms__apiKey`, ...). The database schema is created and migrated on startup.

Clients connect on port 5559 (HTTP) and 5555/5556/5557 (ZeroMQ).

Tests need Docker (they start a throwaway Postgres): `dotnet test QDMS.slnx`.


Architecture
------------
Here's a rough overview of the architecture of QDMS. More details can be found in [architecture.md](https://github.com/qusma/qdms/blob/master/architecture.md).

![Layer Overview](https://qusma.com/images/qdms-architecture-diagram.png)



Contributing
------------

If you wish to contribute, just fork the repo and send a pull request with your changes. Try to send pull requests that are dealing just with one topic - that makes reviewing easier.
Or just create [create an issue](https://github.com/qusma/qdms/issues/new) and we can discuss your great ideas!


License
-------
This product is licensed under the [3-Clause BSD License](LICENSE).
