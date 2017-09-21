// -----------------------------------------------------------------------
// <copyright file="EarningsAnnouncementRequest.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public class EarningsAnnouncementRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="dataLocation"></param>
        /// <param name="symbol">Leave empty to get all symbols</param>
        /// <param name="dataSource">Leave empty to use default</param>
        public EarningsAnnouncementRequest(DateTime fromDate, DateTime toDate, DataLocation dataLocation = DataLocation.LocalOnly, string symbol = null, string dataSource = null)
        {
            FromDate = fromDate;
            ToDate = toDate;
            DataLocation = dataLocation;
            Symbol = symbol;
            DataSource = dataSource;
        }

        //we could make these internal, but requires [JsonConstructor] or ConstructorHandling.AllowNonPublicDefaultConstructor in json.net
        [Obsolete("FOR SERIALIZATION USE ONLY")]
        public EarningsAnnouncementRequest() 
        { }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string Symbol { get; set; }

        public DataLocation DataLocation { get; set; }

        /// <summary>
        /// If this is not specified, the default datasource will be used.
        /// </summary>
        public string DataSource { get; set; }
    }
}