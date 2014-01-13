// -----------------------------------------------------------------------
// <copyright file="RTHFilter.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

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

            return data;
        }
    }
}
