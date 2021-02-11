QUSMA Data Management System (QDMS) [![Build status](https://ci.appveyor.com/api/projects/status/303iq159kj0giugu?svg=true)](https://ci.appveyor.com/project/qusma/qdms)
===================================

QDMS is a client/server system for acquiring, storing, managing, and distributing low-frequency historical and real-time data, written in C#. 

The server acts as a broker between clients and external data sources, as well as a local database of historical data. The server GUI allows its use without the need for a client application.

* [Server Installer](http://qusma.com/QDMS/setup.exe)

A client library is provided which can access the server either locally or over a network, to request data, metadata, etc. A sample application showing usage of the client can be found [here](https://github.com/qusma/qdms/blob/master/SampleApp/Program.cs). Get the client library on NuGet: ![Nuget](https://img.shields.io/nuget/v/QDMSClient)

For bug reports, feature requests, etc. use either the GitHub issue tracker or [gitter chat](https://gitter.im/qusma/community).


Features
--------
* Manage metadata on stocks, options, futures, CFDs, cryptocurrencies, etc.
* Download historical and real time data from external data sources and store it locally.
* Download and store earnings announcements, dividends, economic releases.
* Generate continuous futures data.
* Schedule automatic data updates.


Supported Data Sources
----------------------

| Data Source                           | Historical Data supported | Real Time Data supported |
|-------------------------------------- | ------------------------- | ------------------------ |
| Interactive Brokers                   | :white_check_mark:   		| :white_check_mark:       |
| Binance                               | :white_check_mark:   		| :white_check_mark: |
| Quandl                                | :white_check_mark:   		|                    |
| FRED (Federal Reserve Economic Data)  | :white_check_mark:   		|                    |
| Yahoo                                 | :white_check_mark:   		|                    |
| BarChart                              | :white_check_mark:   		|                    |
| ForexFeed    							| 				    		| :white_check_mark: |
| FXStreet                             |         Economic Announcements Only             		|                    |
| NASDAQ                             |         Dividend Data Only             		|                    |
| CBOE                             |         Earnings Announcements Only             		|                    |
| Bloomberg                             |         WIP             		|                    |
| Tiingo                             |         WIP             		|                    |

Adding more sources is easy and contributions are welcome.


Requirements
------------------------
* MySQL/MariaDB or SQL Server (2008+)
* .NET 4.8 *(.NET Core support planned)*


Screenshots
-----------
| | | |
|:-------------------------:|:-------------------------:|:-------------------------:|
|<a href="https://qusma.com/images/main-server-ui.png"><img alt="Server UI" src="https://qusma.com/images/thumbnails/main-server-ui.png"></a> Server UI |  <a href="https://qusma.com/images/instrument-metadata.png"><img alt="Instrument Metadata" src="https://qusma.com/images/thumbnails/instrument-metadata.png"></a> Instrument metadata|<a href="https://qusma.com/images/continuous-futures-options.png"><img alt="Continuous Futures" src="https://qusma.com/images/thumbnails/continuous-futures-options.png"></a> Continuous futures|
|<a href="https://qusma.com/images/jobs.png"><img alt="Automatic data updates" src="https://qusma.com/images/thumbnails/jobs.png"></a> Scheduling automatic data updates |  <a href="https://qusma.com/images/futures-expiration-rules.png"><img alt="Futures Expiration Rules" src="https://qusma.com/images/thumbnails/futures-expiration-rules.png"></a> Futures expiration rules |<a href="https://qusma.com/images/ib-instrument-addition.png"><img alt="Adding instruments from IB" src="https://qusma.com/images/thumbnails/ib-instrument-addition.png"></a> Adding Instruments from IB|






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
