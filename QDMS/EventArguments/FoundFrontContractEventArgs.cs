// -----------------------------------------------------------------------
// <copyright file="FoundFrontContractEventArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// Used to provide data in response to a <see cref="FrontContractRequest"/>
    /// </summary>
    public class FoundFrontContractEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// 
        /// </summary>
        public Instrument Instrument { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id">Request ID</param>
        /// <param name="instrument">Instrument</param>
        /// <param name="date">Date on which we want to find what the front contract is</param>
        public FoundFrontContractEventArgs(int id, Instrument instrument, DateTime date)
        {
            ID = id;
            Instrument = instrument;
            Date = date;
        }
    }
}
