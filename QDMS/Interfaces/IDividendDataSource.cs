// -----------------------------------------------------------------------
// <copyright file="IDividendDataSource.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QDMS
{
    public interface IDividendDataSource
    {
        /// <summary>
        /// Fires on any error.
        /// </summary>
        event EventHandler<ErrorArgs> Error;

        bool Connected { get; }

        string Name { get; }

        void Connect();

        void Disconnect();

        Task<List<Dividend>> RequestData(DividendRequest request);
    }
}
