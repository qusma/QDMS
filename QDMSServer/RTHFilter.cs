// -----------------------------------------------------------------------
// <copyright file="RTHFilter.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// This class is used to filter data, removing any bars that happen outside of
// regular trading hours.

using System;
using System.Collections.Generic;
using QDMS;

namespace QDMSServer
{
    public static class RTHFilter
    {
        public static List<OHLCBar> Filter(List<OHLCBar> data, List<InstrumentSession> sessions)
        {
            if (data == null) throw new NullReferenceException("data");
            if (sessions == null) throw new NullReferenceException("sessions");
            if (sessions.Count == 0) return data;

            return data;
        }
    }
}
