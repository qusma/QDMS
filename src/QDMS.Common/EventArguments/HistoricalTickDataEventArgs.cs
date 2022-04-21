// -----------------------------------------------------------------------
// <copyright file="HistoricalTickDataEventArgs.cs" company="">
// Copyright 2019 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    /// <summary>
    /// </summary>
    public class HistoricalTickDataEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        public Instrument Instrument { get; }

        /// <summary>
        /// </summary>
        public List<Tick> Ticks { get; }

        /// <summary>
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="ticks"></param>
        public HistoricalTickDataEventArgs(Instrument instrument, List<Tick> ticks)
        {
            Instrument = instrument;
            Ticks = ticks;
        }
    }
}