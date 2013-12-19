// -----------------------------------------------------------------------
// <copyright file="IInstrumentSource.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using EntityData;
using QDMS;

namespace QDMSServer
{
    public interface IInstrumentSource
    {
        /// <summary>
        /// Search for instruments.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="search">Any properties set on this instrument are used as search parameters.
        /// If null, all instruments are returned.</param>
        /// <returns>A list of instruments matching the criteria.</returns>
        List<Instrument> FindInstruments(MyDBContext context = null, Instrument search = null);
    }
}
