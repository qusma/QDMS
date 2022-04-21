// -----------------------------------------------------------------------
// <copyright file="Datasource.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// Used to set where the historical data will be sourced from.
    /// </summary>
    [Serializable]
    public enum DataLocation : int
    {
        /// <summary>
        /// Both external and local data may be returned.
        /// </summary>
        [Description("Both")]
        Both = 0,
        /// <summary>
        /// Forces a fresh download from the external data source.
        /// </summary>
        [Description("External Only")]
        ExternalOnly = 1,
        /// <summary>
        /// Only data from the local database will be returned. 
        /// External datasource will be bypassed.
        /// </summary>
        [Description("Local Only")]
        LocalOnly = 2
    }
}
