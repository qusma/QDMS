// -----------------------------------------------------------------------
// <copyright file="IContinuousFuturesBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// For internal use
    /// </summary>
    public interface IContinuousFuturesBroker : IHistoricalDataSource, IDisposable
    {
        /// <summary>
        /// Finds the currently active futures contract for a continuous futures instrument.
        /// The contract is returned asynchronously through the FoundFrontContract event.
        /// </summary>
        /// <returns>Returns an ID uniquely identifying this request.</returns>
        int RequestFrontContract(Instrument cfInstrument, DateTime? date = null);
        /// <summary>
        /// 
        /// </summary>
        event EventHandler<FoundFrontContractEventArgs> FoundFrontContract;
    }
}
