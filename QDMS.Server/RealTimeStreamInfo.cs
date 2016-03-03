// -----------------------------------------------------------------------
// <copyright file="RealTimeStreamInfo.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// Holds information on a real time data stream. 
// Used to display the "running" streams in the server UI.

using QDMS;

namespace QDMSServer
{
    public struct RealTimeStreamInfo
    {
        public int RequestID { get; set; }
        public string Datasource { get; set; }
        public Instrument Instrument { get; set; }
        public BarSize Frequency { get; set; }
        public bool RTHOnly { get; set; }

        public RealTimeStreamInfo(Instrument instrument, int requestID, string datasource, BarSize frequency, bool rthOnly) : this()
        {
            Instrument = instrument;
            RequestID = requestID;
            Datasource = datasource;
            Frequency = frequency;
            RTHOnly = rthOnly;
        }

        public bool Equals(RealTimeStreamInfo compare)
        {
            bool equal =
                compare.RequestID == RequestID &&
                compare.Datasource == Datasource &&
                compare.Instrument.ID == Instrument.ID &&
                compare.Frequency == Frequency &&
                compare.RTHOnly == RTHOnly;
            return equal;
        }
    }
}