The QUSMA Data Management System (QDMS) is an application for acquiring, managing, and distributing low-frequency historical and real-time data, written in C#. 

QDMS uses a client/server model. The server acts as a broker between clients and external data sources. It also manages metadata on instruments, and local storage of historical data. Finally it also functions as a UI for managing the metadata & data, as well as importing/exporting data from and to CSV files. 

A simple sample application showing usage of the client can be found in the SampleApp project.

QDMS uses MySQL for storage, ZeroMQ and Protocol Buffers for client/server communications, MahApps.Metro for the interface, and ib-csharp to communicate with IB's TWS.

If you wish to contribute, fork the repo and send a pull request with your changes.

Screenshots:
------------------------
* [Instrument metadata](http://i.imgur.com/QACkNxI.png).
* [The main server interface](http://i.imgur.com/i985ZUW.png).
* [Adding a new instrument from IB](http://i.imgur.com/HGPsoK5.png).
* [Importing CSV data](http://i.imgur.com/en6kDo1.png).
* [A rough view of how the systems are connected to each other](http://i.imgur.com/qUWlpj7.png).

Currently Supported Data Sources:
------------------------
* Yahoo
* Interactive Brokers

Requirements:
------------------------
* A reasonably recent version of MySQL.
* .NET 4.5

Planned features/improvements:
------------------------
* Continuous futures.
* Canceling real time data streams.
* Constructing low-frequency bars from higher frequency data.
* Support for more data sources.
* Support for fundamental data.
* Alternative (binary files) storage mechanism for tick data.
* Some sort of market-wide "snapshot" functionality.
* Far wider test coverage.
* Proper docs.