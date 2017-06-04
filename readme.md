QUSMA Data Management System (QDMS)
===================================

[![Join the chat at https://gitter.im/qdms/Lobby](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/qdms/Lobby)
[![Build status](https://ci.appveyor.com/api/projects/status/303iq159kj0giugu?svg=true)](https://ci.appveyor.com/project/qusma/qdms)

The QUSMA Data Management System (QDMS) is a client/server system for acquiring, managing, and distributing low-frequency historical and real-time data, written in C#. 

The server acts as a broker between clients and external data sources, as well as a local database of historical data. The server UI allows its use without the need for a client application.

Here's a rough view of how the systems are connected to each other:

![Layer Overview](http://i.imgur.com/oRbwoiG.png).

A client library is provided which can access the server either locally or over a network, to request data, metadata, etc. A simple sample application showing usage of the client can be found [here](https://github.com/qusma/qdms/blob/master/SampleApp/Program.cs).

QDMS uses MySQL or SQL Server for storage, ZeroMQ and Protocol Buffers for client/server communications and MahApps.Metro for the interface.


Features
--------
* Manage metadata on stocks, options, futures, CFDs, etc.
* Download historical and real time data from external data sources.
* Local storage of historical data.
* Continuous futures data.
* Schedule automatic data updates.
* CSV import/export.


Supported Data Sources
----------------------

| Data Source                           | Historical Data supported | Real Time Data supported |
|-------------------------------------- | ------------------------- | ------------------------ |
| Interactive Brokers                   | :white_check_mark:    |                    |
| Bloomberg                             | :white_check_mark:    | :white_check_mark: |
| Quandl                                | :white_check_mark:    |                    |
| FRED (Federal Reserve Economic Data)  | :white_check_mark:    |                    |
| Yahoo                                 | :white_check_mark:    |                    |
| BarChart                              | :white_check_mark:    |                    |
| [OpenECry](http://futuresonline.com/) | :white_check_mark:    | :white_check_mark: |
| [ForexFeed](http://forexfeed.net/)    | (not implemented)     | :white_check_mark: |

When you miss a data service, feel free to ask...

Requirements:
------------------------
* MySQL/MariaDB or SQL Server (2008+)
* Windows Client
* .NET 4.5 *(.NET Core support planned)*


Screenshots
-----------
* [Instrument metadata](http://i.imgur.com/GXw8amN.png).
* [The main server interface](http://i.imgur.com/i985ZUW.png).
* [Adding a new instrument from IB](http://i.imgur.com/HGPsoK5.png).
* [Importing CSV data](http://i.imgur.com/en6kDo1.png).
* [Editing futures expiration rules](http://i.imgur.com/WvKkb4x.png).
* [Continuous futures options](http://i.imgur.com/47VuXmH.png).


Contributing
------------

If you wish to contribute, you can easily fork the repo and send a pull request with your changes. Try to send pull requests that are dealing just with one topic - that makes reviewing easier.
Or just create [create an issue](https://github.com/qusma/qdms/issues/new) and we can discuss your great ideas!

Roadmap
-------
Take a look at [Roadmap](roadmap.md), the github [issues](https://github.com/qusma/qdms/labels/enhancement) and the [milestones](https://github.com/qusma/qdms/milestones).


License
-------
This product is licensed under the [3-Clause BSD License](LICENSE).
