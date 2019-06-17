// -----------------------------------------------------------------------
// <copyright file="FrontContractRequest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// A request to find what the front contract for a future is, at a particular date
    /// </summary>
    public class FrontContractRequest
    {
        /// <summary>
        /// 
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Instrument Instrument { get; set; }

        /// <summary>
        /// If set to null, the front contract for today will be returned
        /// </summary>
        public DateTime? Date { get; set; }
    }
}
