Here's a rough overview of how the systems are connected to each other. Broadly, requests come in over the network through the servers, and are passed to the approriate broker. Data is then acquired from external data sources and/or the local DB, returned to the broker, sent up to the server, and finally back to the client over the network.

![Layer Overview](http://i.imgur.com/oRbwoiG.png)





## Servers

Network communication happens in two ways: over ZeroMQ and over HTTP(S).

### ZMQ

This method is reserved for queries with high performance requirements, specifically the main data transfer functionalities. The main code is in RealTimeDataServer and HistoricalDataServer, and communication happens using protobuf for serialization and LZ4 for compression. 

### HTTP

QDMS uses NancyFX to run a self-hosted HTTP server. Nancy is configured in CustomBootstrapper and the API endpoints are implemented in QDMS.Server/NancyModules. 


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

QDMS uses Quartz.NET for scheduling automated data updates, with the job details being stored in Quartz's own database. The relevant implementations are in QDMS.Server/Jobs.