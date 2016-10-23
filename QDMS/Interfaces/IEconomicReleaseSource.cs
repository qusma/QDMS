// -----------------------------------------------------------------------
// <copyright file="IEconomicReleaseSource.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QDMS
{
    public interface IEconomicReleaseSource
    {
        Task<List<EconomicRelease>> RequestData(DateTime startDate, DateTime endDate);

        string Name { get; }
        bool Connected { get; }

        void Connect();

        void Disconnect();

        /// <summary>
        /// Fires on any error.
        /// </summary>
        event EventHandler<ErrorArgs> Error;
    }
}