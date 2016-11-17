// -----------------------------------------------------------------------
// <copyright file="IInstrumentSource.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace QDMSServer
{
    public interface IInstrumentSource
    {
        /// <summary>
        /// Search for instruments.
        /// </summary>
        /// <param name="search">Any properties set on this instrument are used as search parameters.
        /// If null, all instruments are returned.</param>
        /// <returns>A list of instruments matching the criteria.</returns>
        Task<List<Instrument>> FindInstruments(Instrument search = null);

        /// <summary>
        /// Search for instruments.
        /// </summary>
        /// <param name="pred">An expression with the instrument search</param>
        /// <returns>A list of instruments matching the criteria.</returns>
        Task<List<Instrument>> FindInstruments(Expression<Func<Instrument, bool>> pred);

        /// <summary>
        /// Add a new instrument or update an existing instrument in the database.
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="saveChanges">Set to true if saving to db should be done.</param>
        /// <returns>True if the insertion or update succeeded. False if it did not.</returns>
        Task<Instrument> AddInstrument(Instrument instrument, bool saveChanges = true);

        /// <summary>
        /// Updates the instrument with new values.
        /// </summary>
        Task UpdateInstrument(Instrument attachedInstrument, Instrument newValues);

        /// <summary>
        /// Delete an instrument and all locally stored data.
        /// </summary>
        Task RemoveInstrument(Instrument instrument, IDataStorage localStorage);
    }
}