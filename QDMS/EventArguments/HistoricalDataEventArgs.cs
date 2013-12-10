// -----------------------------------------------------------------------
// <copyright file="HistoricalDataEventArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    public class HistoricalDataEventArgs : EventArgs
    {
        public HistoricalDataEventArgs(HistoricalDataRequest request, List<OHLCBar> data)
        {
            Request = request;
            Data = data;
        }

        /// <summary>
        /// Parameterless constructor is needed for protobuf-net to properly serialize this object.
        /// </summary>
        /// 
        private HistoricalDataEventArgs()
        {

        }

        public HistoricalDataRequest Request;
        public List<OHLCBar> Data;
    }
}
