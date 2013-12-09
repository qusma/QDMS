The QUSMA Data Management System (QDMS) is an application for acquiring, managing, and distributing low-frequency historical and real-time data, written in C#. QDMS uses a client/server model and has the ability to distribute data both locally and over a network.

The server manages metadata on instruments, acquires data from external data sources, manages local storage of historical data, and acts as a "router" for real time data streams. The server application also functions as a UI for managing the data, as well as importing/exporting data from and to CSV files. 

A simple sample application showing usage of the client can be found in the SampleApp project.

QDMS uses MySQL for storage, ZeroMQ and Protocol Buffers for client/server communications, MahApps.Metro for the interface, and ib-csharp to communicate with IB's TWS.

If you wish to contribute, fork the repo and send a pull request with your changes.

Screenshots:
------------------------
* [Instrument metadata](http://i.imgur.com/QACkNxI.png).
* [The main server interface](http://i.imgur.com/i985ZUW.png).
* [Adding a new instrument from IB](http://i.imgur.com/HGPsoK5.png).
* [Importing CSV data](http://i.imgur.com/en6kDo1.png).

Currently Supported Data Sources:
------------------------
* Yahoo
* Interactive Brokers

Requirements:
------------------------
* A reasonably recent version of MySQL.

Planned features/improvements:
------------------------
* Continuous futures.
* Canceling real time data streams.
* Constructing low-frequency bars from higher frequency data.
* Support for more data sources.
* Support for fundamental data.
* Alternative (binary files) storage mechanism for tick data.
* Far wider test coverage.
* Proper docs.