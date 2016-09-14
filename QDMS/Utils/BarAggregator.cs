// -----------------------------------------------------------------------
// <copyright file="BarAggregator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace QDMS
{
    public static class BarAggregator
    {
        /// <summary>
        /// Combines higher frequency bars into lower frequency bars
        /// </summary>
        /// <param name="data"></param>
        /// <param name="targetFrequency"></param>
        /// <returns></returns>
        public static List<OHLCBar> Aggregate(IEnumerable<OHLCBar> data, BarSize targetFrequency)
        {
            if (data == null) throw new ArgumentException(nameof(data));

            double tgtFreqLength = targetFrequency.ToTimeSpan().TotalSeconds * 10000000;

            return data
                .GroupBy(x => Math.Floor(x.DTOpen.Value.Ticks / tgtFreqLength))
                .Select(x => x.OrderBy(y => y.DT))
                .Select(x => new OHLCBar
                {
                    Open = x.First().Open,
                    High = x.Max(y => y.High),
                    Low = x.Min(y => y.Low),
                    Close = x.Last().Close,
                    DTOpen = x.First().DTOpen,
                    DT = x.Last().DT,
                    Volume = x.Sum(y => y.Volume)
                }
                )
                .ToList();

        }
    }
}
