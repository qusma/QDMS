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
    /// <summary>
    /// Interface for economic release data sources
    /// </summary>
    public interface IEconomicReleaseSource
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        Task<List<EconomicRelease>> RequestData(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// 
        /// </summary>
        void Connect();

        /// <summary>
        /// 
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Fires on any error.
        /// </summary>
        event EventHandler<ErrorArgs> Error;
    }
}