Here's a rough overview of how the systems are connected to each other. 

![Layer Overview](https://qusma.com/images/qdms-architecture-diagram.png)


Requests come in over the network through the servers, and are passed to the approriate broker. Data is then acquired from external data sources and/or the local DB, returned to the broker, potentially stored to the DB, then sent up to the server, and finally back to the client over the network.


## Servers

Network communication happens in two ways: over ZeroMQ and over HTTP(S).

### ZMQ

This method is reserved for queries with high performance requirements, specifically the main data transfer functionalities. The main code is in RealTimeDataServer and HistoricalDataServer. The messages are serialized using protobuf and compressed with LZ4.

### HTTP

Metadata (instruments, exchanges, sessions, tags, datasources), dividends, earnings, economic releases and job management go over an HTTP API,
built with ASP.NET Core Minimal APIs. The endpoints are in QDMS.Server/Http, and QDMS.Host wires them up. Requests authenticate with an API key
in the Authorization header.


## Brokers

Requests are forwarded from the servers to intermediary brokers, which pass on requests to the appropriate external data sources and/or the local database, process the results, and then return the data to the server. They can be found in QDMS.Server/Brokers:

 * HistoricalDataBroker
 * RealTimeDataBroker
 * ContinuousFuturesBroker
 * EarningsAnnouncementBroker
 * EconomicReleaseBroker
 * DividendsBroker


## Datasources

Datasources receive requests from brokers and fulfill them by querying external data sources. Each datasource gets its own project.


## Scheduling

QDMS uses Quartz.NET for scheduling automated data updates, with the job details stored in Quartz's own tables (`qrtz_*`) in the metadata database. The relevant implementations are in QDMS.Server/Jobs.


## Storage

Metadata lives in PostgreSQL via EF Core (QDMS.Dal, `MyDBContext`). Bars are stored in the same database by default (the `bars` and `instrument_info`
tables, schema from `DataDBContext`), read and written by `PostgresStorage` with plain Npgsql for speed.


## Hosting

QDMS.Host is an ASP.NET Core application that composes everything: configuration, dependency injection, database migrations and seeding at startup,
the ZMQ servers as a hosted service, Quartz, and the HTTP API.