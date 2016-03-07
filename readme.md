Note:
This is a fork of the QDMS from QUSMA. In general, my plan was not to make my own fork. I would gladly see my changes in the origin fork. But the fact that QUSMA seems to be dead forced me to make my own fork.

My maior changes to the origin fork are:
* refactoring, seperating logic to seperate assemblies (to make it more flexible to the individual need)
* adding new data sources.
* minor improvements.

---- 

The QUSMA Data Management System (QDMS) is a client/server system for acquiring, managing, and distributing low-frequency historical and real-time data, written in C#. 

The server acts as a broker between clients and external data sources, as well as a local database of historical data. The server UI allows its use without the need for a client application. [Here's a rough view of how the systems are connected to each other](http://i.imgur.com/oRbwoiG.png).

A client library is provided which can access the server either locally or over a network, to request data, metadata, etc. A simple sample application showing usage of the client can be found [here](https://github.com/qusma/qdms/blob/master/SampleApp/Program.cs).

QDMS uses MySQL or SQL Server for storage, ZeroMQ and Protocol Buffers for client/server communications, MahApps.Metro for the interface, and ib-csharp to communicate with IB's TWS.

If you wish to contribute, fork the repo and send a pull request with your changes.

For bug reports, feature requests, and general discussion please use the [google group](https://groups.google.com/forum/#!forum/qusma-data-management-system).

Features:
------------------------
* Manage metadata on stocks, options, futures, CFDs, etc.
* Download historical and real time data from external data sources.
* Local storage of historical data.
* Continuous futures data.
* Schedule automatic data updates.
* CSV import/export.

Screenshots:
------------------------
* [Instrument metadata](http://i.imgur.com/GXw8amN.png).
* [The main server interface](http://i.imgur.com/i985ZUW.png).
* [Adding a new instrument from IB](http://i.imgur.com/HGPsoK5.png).
* [Importing CSV data](http://i.imgur.com/en6kDo1.png).
* [Editing futures expiration rules](http://i.imgur.com/WvKkb4x.png).
* [Continuous futures options](http://i.imgur.com/47VuXmH.png).

Currently Supported Data Sources:
------------------------

| Data Source | Historical Data supported | Real Time Data supported | Verified and Tested |
| ----------- | ------------------------- | ------------------------ | ------------------- |
| Yahoo       | [x] | [ ] | [ ] |
| Interactive Brokers | [x] | [ ] | [ ] |
| Quandl | [x] | [ ] | [ ] |
| FRED (Federal Reserve Economic Data) | [x] | [ ] | [ ] |
| Google Finance | [x] | [ ] | [ ] |
| Bloomberg | [x] | [x] | [ ] |
| [OpenECry](http://futuresonline.com/) | [x] | [x] | [ ] |
| ForexFeed | [ ] | [x] | [x] |

Requirements:
------------------------
* MySQL/MariaDB or SQL Server (2008+)
* Windows Client (currently no Mono suport)
* .NET 4.5

Planned features/improvements (from QUSMA):
------------------------
* Excel plugin.
* Constructing low-frequency bars from higher frequency data.
* Support for more data sources.
* Support for fundamental data.
* Alternative (binary files) storage mechanism for tick data.
* Some sort of market-wide "snapshot" functionality.
* Proper docs.
