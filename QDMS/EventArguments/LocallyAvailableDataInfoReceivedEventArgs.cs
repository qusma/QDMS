// -----------------------------------------------------------------------
// <copyright file="LocallyAvailableDataInfoReceivedEventArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    /// <summary>
    /// 
    /// </summary>
    public class LocallyAvailableDataInfoReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// The instrument the request is for.
        /// </summary>
        public Instrument Instrument { get; set; }

        /// <summary>
        /// List of StoredDataInfo objects, containing available frequencies and covered dates.
        /// </summary>
        public List<StoredDataInfo> StorageInfo { get; set; }

        /// <summary>
        /// Event args for locally stored data info requests.
        /// </summary>
        public LocallyAvailableDataInfoReceivedEventArgs(Instrument instrument, List<StoredDataInfo> storageInfo)
        {
            Instrument = instrument;
            StorageInfo = storageInfo;
        }
    }
}
