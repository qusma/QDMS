# QUSMA Data Management System (QDMS) fork
[![Build Status](https://travis-ci.org/leo90skk/qdms.svg?branch=master)](https://travis-ci.org/leo90skk/qdms)

The QUSMA Data Management System (QDMS) is a client/server system for acquiring, managing, and distributing low-frequency historical and real-time data, written in C#. 

The server acts as a broker between clients and external data sources, as well as a local database of historical data. The server UI allows its use without the need for a client application.

Here's a rough view of how the systems are connected to each other:

![Layer Overview](http://i.imgur.com/oRbwoiG.png).

A client library is provided which can access the server either locally or over a network, to request data, metadata, etc. A simple sample application showing usage of the client can be found [here](https://github.com/leo90skk/qdms/blob/master/SampleApp/Program.cs).

QDMS uses MySQL or SQL Server for storage, ZeroMQ and Protocol Buffers for client/server communications and MahApps.Metro for the interface.


## Features

* Manage metadata on stocks, options, futures, CFDs, etc.
* Download historical and real time data from external data sources.
* Local storage of historical data.
* Continuous futures data.
* Schedule automatic data updates.
* CSV import/export.


## Screenshots

* [Instrument metadata](http://i.imgur.com/GXw8amN.png).
* [The main server interface](http://i.imgur.com/i985ZUW.png).
* [Adding a new instrument from IB](http://i.imgur.com/HGPsoK5.png).
* [Importing CSV data](http://i.imgur.com/en6kDo1.png).
* [Editing futures expiration rules](http://i.imgur.com/WvKkb4x.png).
* [Continuous futures options](http://i.imgur.com/47VuXmH.png).


## Supported Data Sources

| Data Source                           | Historical Data supported | Real Time Data supported | Verified and Tested |
|-------------------------------------- | ------------------------- | ------------------------ | ------------------- |
| Yahoo                                 | :white_check_mark:    |                    | :grey_question: |
| Interactive Brokers                   | :white_check_mark:    |                    | :grey_question: |
| Quandl                                | :white_check_mark:    |                    | :grey_question: |
| FRED (Federal Reserve Economic Data)  | :white_check_mark:    |                    | :grey_question: |
| Bloomberg                             | :white_check_mark:    | :white_check_mark: | :grey_question: |
| [OpenECry](http://futuresonline.com/) | :white_check_mark:    | :white_check_mark: | :grey_question: |
| [ForexFeed](http://forexfeed.net/)    | (not implemented yet) | :white_check_mark: | :white_check_mark: |

Feel free to add a new data service that you're missing. Please make a pull request when you're finish.

Requirements:
------------------------
* MySQL/MariaDB or SQL Server (2008+)
* Windows Client
* .NET 4.5 *(.NET Core support planned)*


## Roadmap

Take a look at [Roadmap](roadmap.md), the github [issues](https://github.com/leo90skk/qdms/labels/enhancement) and the [milestones](https://github.com/leo90skk/qdms/milestones).


## Contributing

If you wish to contribute, you can easily fork the repo and send a pull request with your changes. Try to send pull requests that are dealing just with one topic.
In case you recommend a bigger changes (like general architecture changes), please create an issue for that and we can discuss the best way of doing it.

For bug reports, feature requests, and general discussion please use the github issue list for this fork.


## Why this fork

This is a fork of the QDMS from QUSMA. In general, my plan was not to make my own fork. I would gladly see my changes in the origin fork. But the fact that QUSMA seems to be dead forced me to make my own fork.
I wanna help to build a community around the QDMS project.

My major changes to the origin fork are:
* refactoring, splitting logic to separate assemblies (to make it more flexible to the individual need)
* adding new data sources.
* minor improvements.
* see also github [issues list](https://github.com/leo90skk/qdms/issues)


## Feedback

Please send all your feedback / bug report etc. via the [GitHub Issues](https://github.com/leo90skk/qdms/issues) list.

## License
This project is still licensed on the [origin license](LICENSE) from the QDMS project from QUSMA.
