// -----------------------------------------------------------------------
// <copyright file="TickStorage.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// A class for storing tick data in compressed binary files.
// One file per day per instrument with time offsets in the header for easy searching.
// LZ4 for compression? Not sure what the desirable speed:compression tradeoff is in this case.

using System;
using System.Collections.Generic;
using QDMS;

namespace QDMSServer.DataSources
{
    public class TickStorage
    {
    }
}
