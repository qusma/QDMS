// -----------------------------------------------------------------------
// <copyright file="FrontContractRequest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public class FrontContractRequest
    {
        public int ID { get; set; }
        public Instrument Instrument { get; set; }
        public DateTime? Date { get; set; }
    }
}
