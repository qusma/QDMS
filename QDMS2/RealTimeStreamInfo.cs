// -----------------------------------------------------------------------
// <copyright file="RealTimeStreamInfo.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS
{
    /// <summary>
    ///     Holds information on a real time data stream.
    ///     Used to display the "running" streams in the server UI.
    /// </summary>
    public struct RealTimeStreamInfo
    {
        /// <summary>
        /// </summary>
        public int RequestID { get; set; }

        /// <summary>
        /// </summary>
        public string Datasource { get; set; }

        /// <summary>
        /// </summary>
        public Instrument Instrument { get; set; }

        /// <summary>
        /// </summary>
        public BarSize Frequency { get; set; }

        /// <summary>
        /// </summary>
        public bool RTHOnly { get; set; }

        /// <summary>
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="requestID"></param>
        /// <param name="datasource"></param>
        /// <param name="frequency"></param>
        /// <param name="rthOnly"></param>
        public RealTimeStreamInfo(Instrument instrument, int requestID, string datasource, BarSize frequency,
            bool rthOnly) : this()
        {
            Instrument = instrument;
            RequestID = requestID;
            Datasource = datasource;
            Frequency = frequency;
            RTHOnly = rthOnly;
        }

        /// <summary>
        /// </summary>
        /// <param name="compare"></param>
        /// <returns></returns>
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