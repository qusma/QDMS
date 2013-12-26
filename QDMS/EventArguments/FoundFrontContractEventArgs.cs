// -----------------------------------------------------------------------
// <copyright file="FoundFrontContractEventArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public class FoundFrontContractEventArgs : EventArgs
    {
        public int ID { get; private set; }
        public Instrument Instrument { get; private set; }
        public DateTime Date { get; private set; }

        public FoundFrontContractEventArgs(int id, Instrument instrument, DateTime date)
        {
            ID = id;
            Instrument = instrument;
            Date = date;
        }
    }
}
