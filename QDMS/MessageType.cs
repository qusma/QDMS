// -----------------------------------------------------------------------
// <copyright file="MessageType.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS
{
    /// <summary>
    /// Holds message type strings used for communicating between clients and server
    /// </summary>
    public static class MessageType
    {
        /// <summary>
        /// Push historical data from client to server
        /// </summary>
        public const string HistPush = "HISTPUSH";

        /// <summary>
        /// Reply to push request.
        /// </summary>
        public const string HistPushReply = "PUSHREP";

        /// <summary>
        /// Historical data request
        /// </summary>
        public const string HistRequest = "HISTREQ";

        /// <summary>
        /// Reply to historical data request
        /// </summary>
        public const string HistReply = "HISTREQREP";

        /// <summary>
        /// Real time data request
        /// </summary>
        public const string RTDRequest = "RTDREQ";

        /// <summary>
        /// Cancel real time data stream
        /// </summary>
        public const string CancelRTD = "CANCEL";

        /// <summary>
        /// Successful real time data stream cancelation
        /// </summary>
        public const string RTDCanceled = "CANCELED";

        /// <summary>
        /// Instrument search request
        /// </summary>
        public const string Search = "SEARCH";

        /// <summary>
        /// Instrument search request with a predicate
        /// </summary>
        public const string PredicateSearch = "PREDSEARCH";

        /// <summary>
        /// Request for all instruments
        /// </summary>
        public const string AllInstruments = "ALL";

        /// <summary>
        /// Request to add an instrument to the db
        /// </summary>
        public const string AddInstrument = "ADD";

        /// <summary>
        /// Sent ahead of real time bar messages
        /// </summary>
        public const string RealTimeBars = "B";

        /// <summary>
        /// Sent ahead of real time tick messages
        /// </summary>
        public const string RealTimeTick = "T";

        /// <summary>
        /// 
        /// </summary>
        public const string Ping = "PING";
        /// <summary>
        /// 
        /// </summary>
        public const string Pong = "PONG";
        /// <summary>
        /// 
        /// </summary>
        public const string Error = "ERROR";
        /// <summary>
        /// 
        /// </summary>
        public const string Success = "SUCCESS";
    }
}
