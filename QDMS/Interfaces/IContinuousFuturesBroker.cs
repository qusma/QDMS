// -----------------------------------------------------------------------
// <copyright file="IContinuousFuturesBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public interface IContinuousFuturesBroker : IHistoricalDataSource
    {
        /// <summary>
        /// Finds the currently active futures contract for a continuous futures instrument.
        /// The contract is returned asynchronously through the FoundFrontContract event.
        /// </summary>
        /// <returns>Returns an ID uniquely identifying this request.</returns>
        int RequestFrontContract(Instrument cfInstrument, DateTime? date = null);
        event EventHandler<FoundFrontContractEventArgs> FoundFrontContract;
    }
}
