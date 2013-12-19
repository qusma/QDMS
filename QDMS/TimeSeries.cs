// -----------------------------------------------------------------------
// <copyright file="TimeSeries.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace QDMS
{
    public class TimeSeries
    {
        public TimeSeries(IEnumerable<OHLCBar> data)
        {
            Series = new List<OHLCBar>(data);
        }

        /// <summary>
        /// The data series, which is held in a list.
        /// </summary>
        protected List<OHLCBar> Series;

        /// <summary>
        /// Get the value of the current bar.
        /// </summary>
        public int CurrentBar { get; private set; }

        protected int PreviousBar;

        /// <summary>
        /// Length of the variable series.
        /// </summary>
        public int Count
        {
            get
            {
                return Series.Count;
            }
        }

        /// <summary>
        /// Indexer access to current and past values of the VariableSeries.
        /// </summary>
        public OHLCBar this[int index]
        {
            get
            {
                try
                {
                    return Series[CurrentBar - index];
                }
                catch (Exception ex)
                {
                    throw new ArgumentOutOfRangeException(ex.Message);
                }
            }
        }

        /// <summary>
        /// Provides get/set access to the "current" value of the VariableSeries.
        /// </summary>
        public OHLCBar Value
        {
            get
            {
                return Series[CurrentBar];
            }

            set
            {
                Series[CurrentBar] = value;
            }
        }

        /// <summary>
        /// Progress the "current" time of the VariableSeries to the specified date and time.
        /// </summary>
        /// <returns>True if the CurrentBar was changed</returns>
        public bool AdvanceTo(DateTime target)
        {
            if (CurrentBar == Series.Count - 1) return false;
            if (Series[CurrentBar + 1].DT <= target)
            {
                PreviousBar = CurrentBar;
                CurrentBar++;
                //loop until we have reached the last possible date that is before or equal to the target
                while (CurrentBar + 1 < Series.Count && Series[CurrentBar + 1].DT <= target)
                {
                    CurrentBar++;
                }

                return true;
            }
            
            
            return false;
        }
    }
}
