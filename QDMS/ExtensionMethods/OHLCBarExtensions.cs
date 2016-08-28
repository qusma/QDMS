// -----------------------------------------------------------------------
// <copyright file="OHLCBarExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace QDMS
{
    public static class OHLCBarExtensions
    {
        /// <summary>
        /// Save a collection of OHLCBars to a file, in CSV format.
        /// </summary>
        public static void ToCSVFile(this IEnumerable<OHLCBar> data, string filePath)
        {
            using (StreamWriter file = new StreamWriter(filePath))
            {
                //write header first
                var headerFields = new List<string>
                {
                    "DateTime Open",
                    "Date",
                    "Time",
                    "Open",
                    "High",
                    "Low",
                    "Close",
                    "Volume",
                    "Open Interest",
                    "Dividend",
                    "Split",
                    "AdjOpen",
                    "AdjHigh",
                    "AdjLow",
                    "AdjClose"
                };
                string header = String.Join(",", headerFields);
                file.WriteLine(header);

                foreach (OHLCBar bar in data)
                {
                    file.WriteLine("{14},{0:yyyy-MM-dd},{1:HH:mm:ss.fff},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13}",
                        bar.Date,
                        bar.Date,
                        bar.Open,
                        bar.High,
                        bar.Low,
                        bar.Close,
                        bar.Volume,
                        bar.OpenInterest,
                        bar.Dividend,
                        bar.Split,
                        bar.AdjOpen,
                        bar.AdjHigh,
                        bar.AdjLow,
                        bar.AdjClose,
                        bar.DTOpen);
                }
            }
        }
    }
}
