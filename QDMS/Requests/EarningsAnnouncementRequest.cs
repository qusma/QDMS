// -----------------------------------------------------------------------
// <copyright file="EarningsAnnouncementRequest.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    /// <summary>
    /// Request for earnings announcement data
    /// </summary>
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
            Symbol = new List<string>();
            if (symbol != null)
            {
                Symbol.Add(symbol);
            }
            DataSource = dataSource;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="dataLocation"></param>
        /// <param name="symbol">Leave empty to get all symbols</param>
        /// <param name="dataSource">Leave empty to use default</param>
        public EarningsAnnouncementRequest(DateTime fromDate, DateTime toDate, List<string> symbol, DataLocation dataLocation = DataLocation.LocalOnly, string dataSource = null)
        {
            FromDate = fromDate;
            ToDate = toDate;
            DataLocation = dataLocation;
            Symbol = symbol;
            DataSource = dataSource;
        }

        //we could make these internal, but requires [JsonConstructor] or ConstructorHandling.AllowNonPublicDefaultConstructor in json.net
        /// <summary>
        /// 
        /// </summary>
        [Obsolete("FOR SERIALIZATION USE ONLY")]
        public EarningsAnnouncementRequest() 
        { }

        /// <summary>
        /// 
        /// </summary>
        public DateTime FromDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime ToDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public List<string> Symbol { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DataLocation DataLocation { get; set; }

        /// <summary>
        /// If this is not specified, the default datasource will be used.
        /// </summary>
        public string DataSource { get; set; }
    }
}