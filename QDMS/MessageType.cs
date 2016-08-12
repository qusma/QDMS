// -----------------------------------------------------------------------
// <copyright file="MessageType.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS
{
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
        /// Request for locally available data
        /// </summary>
        public const string AvailableDataRequest = "AVAILABLEDATAREQ";

        /// <summary>
        /// Locally available data reply
        /// </summary>
        public const string AvailableDataReply = "AVAILABLEDATAREP";

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

        public const string Ping = "PING";
        public const string Pong = "PONG";
        public const string Error = "ERROR";
        public const string Success = "SUCCESS";
    }
}
