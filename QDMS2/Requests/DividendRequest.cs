// -----------------------------------------------------------------------
// <copyright file="DividendRequest.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    /// <summary>
    /// Request for dividend data
    /// </summary>
    public class DividendRequest
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromDate"></param>
        /// <param name="toDate"></param>
        /// <param name="dataLocation"></param>
        /// <param name="symbol">Leave empty to get all symbols</param>
        /// <param name="dataSource">Leave empty to use default</param>
        public DividendRequest(DateTime fromDate, DateTime toDate, DataLocation dataLocation = DataLocation.LocalOnly, string symbol = null, string dataSource = null)
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
        /// <param name="symbols">Request multiple symbols at once</param>
        /// <param name="dataSource">Leave empty to use default</param>
        public DividendRequest(DateTime fromDate, DateTime toDate, List<string> symbols, DataLocation dataLocation = DataLocation.LocalOnly, string dataSource = null)
        {
            FromDate = fromDate;
            ToDate = toDate;
            DataLocation = dataLocation;
            Symbol = symbols;
            DataSource = dataSource;
        }

        /// <summary>
        /// 
        /// </summary>
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
        public string DataSource { get; set;  }
    }
}