// -----------------------------------------------------------------------
// <copyright file="DataSourceStatus.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS
{
    public class DataSourceStatus
    {
        public DataSourceStatus()
        {
        }

        public string Name { get; set; }

        /// <summary>
        /// Set to true if connected, false is not, null if N/A
        /// </summary>
        public bool? RealtimeConnected { get; set; }

        /// <summary>
        /// Set to true if connected, false is not, null if N/A
        /// </summary>
        public bool? HistoricalConnected { get; set; }

        /// <summary>
        /// Set to true if connected, false is not, null if N/A
        /// </summary>
        public bool? EconReleasesConnected { get; set; }
    }
}