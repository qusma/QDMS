// -----------------------------------------------------------------------
// <copyright file="IInstrumentSource.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
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

        /// <summary>
        /// Search for instruments.
        /// </summary>
        /// <param name="pred">An expression with the instrument search</param>
        /// <param name="context"></param>
        /// <returns>A list of instruments matching the criteria.</returns>
        List<Instrument> FindInstruments(Expression<Func<Instrument, bool>> pred, MyDBContext context = null);

        /// <summary>
        /// Tries to add multiple instruments to the database.
        /// </summary>
        /// <returns>The number of instruments that were successfully added.</returns>
        int AddInstruments(IList<Instrument> instruments, bool updateIfExists = false);

        /// <summary>
        /// Add a new instrument or update an existing instrument in the database.
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="updateIfExists"></param>
        /// <param name="saveChanges">Set to true if saving to db should be done.</param>
        /// <returns>True if the insertion or update succeeded. False if it did not.</returns>
        Instrument AddInstrument(Instrument instrument, bool updateIfExists = false, bool saveChanges = true);
    }
}
