// -----------------------------------------------------------------------
// <copyright file="LocallyAvailableDataInfoReceivedEventArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    public class LocallyAvailableDataInfoReceivedEventArgs : EventArgs
    {
        public Instrument Instrument { get; set; }
        public List<StoredDataInfo> StorageInfo { get; set; }

        public LocallyAvailableDataInfoReceivedEventArgs(Instrument instrument, List<StoredDataInfo> storageInfo)
        {
            Instrument = instrument;
            StorageInfo = storageInfo;
        }
    }
}
