// -----------------------------------------------------------------------
// <copyright file="PriceAdjuster.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using QDMS;

namespace QDMSServer
{
    /// <summary>
    /// Takes OHLC bars and produces adjusted prices for dividends and splits.
    /// </summary>
    public static class PriceAdjuster
    {
        /// <summary>
        /// Adjusts OHLC data for dividends and splits.
        /// </summary>
        public static void AdjustData(ref List<OHLCBar> data)
        {
            if (data == null) return;

            //final adjusted prices equal the unadjusted ones
            data[data.Count - 1].AdjOpen = data[data.Count - 1].Open;
            data[data.Count - 1].AdjHigh = data[data.Count - 1].High;
            data[data.Count - 1].AdjLow = data[data.Count - 1].Low;
            data[data.Count - 1].AdjClose = data[data.Count - 1].Close;

            //the idea is to calculate the "correct" total return including dividends and splits
            //and then generate an adjusted price on the previous bar that corresponds to that return
            for (int i = data.Count - 2; i >= 0; i--)
            {
                decimal adjRet = (data[i + 1].Close + (data[i + 1].Dividend ?? 0)) / (data[i].Close / (data[i + 1].Split ?? 1));
                if (adjRet == 0) //corner case when prices go to zero
                {
                    data[i].AdjClose = data[i].Close;
                    data[i].AdjOpen = data[i].Open;
                    data[i].AdjHigh = data[i].High;
                    data[i].AdjLow = data[i].Low;
                    continue;
                }

                data[i].AdjClose = data[i + 1].AdjClose / adjRet;
                decimal ratio = (decimal)(data[i].AdjClose / data[i].Close);
                data[i].AdjOpen = data[i].Open * ratio;
                data[i].AdjHigh = data[i].High * ratio;
                data[i].AdjLow = data[i].Low * ratio;
            }
        }
    }
}
