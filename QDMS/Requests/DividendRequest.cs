// -----------------------------------------------------------------------
// <copyright file="DividendRequest.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public class DividendRequest
    {
        public DividendRequest(DateTime fromDate, DateTime toDate, DataLocation dataLocation = DataLocation.LocalOnly, string symbol = null, string dataSource = null)
        {
            FromDate = fromDate;
            ToDate = toDate;
            DataLocation = dataLocation;
            Symbol = symbol;
            DataSource = dataSource;
        }

        [Obsolete("FOR SERIALIZATION USE ONLY")]
        public DividendRequest()
        { }

        /// <summary>
        /// Cutoff for the ex-date
        /// </summary>
        public DateTime FromDate { get; set; }

        /// <summary>
        /// Cutoff for the ex-date
        /// </summary>
        public DateTime ToDate { get; set; }

        public string Symbol { get; set; }

        public DataLocation DataLocation { get; set; }

        /// <summary>
        /// If this is not specified, the default datasource will be used.
        /// </summary>
        public string DataSource { get; }
    }
}