// -----------------------------------------------------------------------
// <copyright file="DataSourceDisconnectEventArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public class DataSourceDisconnectEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public string Message { get; set; }

        public DataSourceDisconnectEventArgs(string sourceName, string message)
        {
            SourceName = sourceName;
            Message = message;
        }
    }
}
