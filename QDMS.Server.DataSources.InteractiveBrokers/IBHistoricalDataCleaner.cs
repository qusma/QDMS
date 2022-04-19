// -----------------------------------------------------------------------
// <copyright file="IBHistoricalDataCleaner.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using System;
using System.Collections.Generic;
using System.Linq;


namespace QDMSApp.DataSources
{
    internal static class IBHistoricalDataCleaner
    {
        internal static List<OHLCBar> CleanHistoricalData(HistoricalDataRequest request, List<OHLCBar> bars)
        {
            //IB doesn't actually allow us to provide a deterministic starting point for our historical data query
            //so sometimes we get data from earlier than we want
            //here we throw it away
            var cutoffDate = request.StartingDate.Date;


            //due to the nature of sub-requests, the list may contain the same points multiple times
            //so we grab unique values only
            bars = bars.Distinct((x, y) => x.DTOpen == y.DTOpen).ToList();

            //we have to make adjustments to the times as well as derive the bar closing times
            AdjustBarTimes(bars, request);

            //if the data is daily or lower freq, and a stock, set adjusted ohlc values for convenience
            if (request.Frequency >= BarSize.OneDay &&
                request.Instrument.Type == InstrumentType.Stock)
            {
                foreach (OHLCBar b in bars)
                {
                    b.AdjOpen = b.Open;
                    b.AdjHigh = b.High;
                    b.AdjLow = b.Low;
                    b.AdjClose = b.Close;
                }
            }

            return bars.Where(x => x.DT.Date >= cutoffDate).ToList();
        }

        /// <summary>
        /// Fixes bar closing times
        /// </summary>
        private static void AdjustBarTimes(List<OHLCBar> bars, HistoricalDataRequest request)
        {
            //One day or lower frequency means we don't get time data.
            //Instead we provide our own by using that day's session end...
            if (request.Frequency < BarSize.OneDay)
            {
                GenerateIntradayBarClosingTimes(bars, request.Frequency);
            }
            else
            {
                AdjustDailyBarTimes(bars);
            }
        }

        /// <summary>
        /// Sets closing times
        /// </summary>
        private static void AdjustDailyBarTimes(IEnumerable<OHLCBar> bars)
        {
            // For daily data, IB does not provide us with bar opening/closing times.
            // But the IB client does shift the timezone from UTC to local.
            // So to get the correct day we have to shift it back to UTC first.
            foreach (OHLCBar bar in bars)
            {
                bar.DT = bar.DTOpen.Value;
            }
        }


        /// <summary>
        /// Sets the appropriate closing time for each bar, since IB only gives us the opening time.
        /// </summary>
        private static void GenerateIntradayBarClosingTimes(List<OHLCBar> bars, BarSize frequency)
        {
            TimeSpan freqTS = frequency.ToTimeSpan();
            for (int i = 0; i < bars.Count; i++)
            {
                var bar = bars[i];

                if (i == bars.Count - 1)
                {
                    //if it's the last bar we are basically just guessing the 
                    //closing time by adding the duration of the frequency
                    bar.DT = bar.DTOpen.Value + freqTS;
                }
                else
                {
                    //if it's not the last bar, we set the closing time to the
                    //earliest of the open of the next bar and the period of the frequency
                    //e.g. if hourly bar opens at 9:30 and the next bar opens at 10:00
                    //we set the close at the earliest of 10:00 and 10:30
                    DateTime openPlusBarSize = bar.DTOpen.Value + freqTS;
                    bar.DT = bars[i + 1].DTOpen.Value < openPlusBarSize ? bars[i + 1].DTOpen.Value : openPlusBarSize;
                }

            }
        }
    }
}
