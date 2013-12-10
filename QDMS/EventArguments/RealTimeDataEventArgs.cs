// -----------------------------------------------------------------------
// <copyright file="RealTimeDataEventArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using ProtoBuf;

namespace QDMS
{
    [Serializable]
    [ProtoContract]
    public class RealTimeDataEventArgs : EventArgs
    {
        // Summary:
        //     Real Time Bar Event Arguments

        //
        // Summary:
        //     Real Time Bar Event Arguments
        //
        // Parameters:
        //   requestId:
        //     The ticker Id of the request to which this bar is responding.
        //
        //   time:
        //     The date-time stamp of the start of the bar.  The format is determined by
        //     the reqHistoricalData() formatDate parameter.
        //
        //   open:
        //     Bar opening price.
        //
        //   high:
        //     High price during the time covered by the bar.
        //
        //   low:
        //     Low price during the time covered by the bar.
        //
        //   close:
        //     Bar closing price.
        //
        //   volume:
        //     Volume during the time covered by the bar.
        //
        //   wap:
        //     Weighted average price during the time covered by the bar.
        //
        //   count:
        //     When TRADES historical data is returned, represents the number of trades
        //     that occurred during the time period the bar covers.
        public RealTimeDataEventArgs(string symbol, long time, decimal open, decimal high, decimal low, decimal close, long volume, double wap, int count)
        {
            Symbol = symbol;
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Wap = wap;
            Count = count;
        }

        /// <summary>
        /// Parameterless constructor is needed for protobuf-net to properly serialize this object.
        /// </summary>
        private RealTimeDataEventArgs()
        {

        }

        /// <summary>
        /// Bar closing price.
        /// </summary>
        [ProtoMember(1)]
        public decimal Close { get; set; }

        /// <summary>
        /// When TRADES historical data is returned, represents the number of trades that occurred during the time period the bar covers.
        /// </summary>
        [ProtoMember(2)]
        public int Count { get; set; }

        /// <summary>
        /// High price during the time covered by the bar.
        /// </summary>
        [ProtoMember(3)]
        public decimal High { get; set; }

        /// <summary>
        /// Low price during the time covered by the bar.
        /// </summary>
        [ProtoMember(4)]
        public decimal Low { get; set; }

        /// <summary>
        /// Bar opening price.
        /// </summary>
        [ProtoMember(5)]
        public decimal Open { get; set; }

        /// <summary>
        /// The symbol of the instrument.
        /// </summary>
        [ProtoMember(6)]
        public string Symbol { get; set; }

        /// <summary>
        /// The date-time stamp of the start of the bar.
        /// </summary>
        [ProtoMember(7)]
        public long Time { get; set; }

        /// <summary>
        /// Volume during the time covered by the bar.
        /// </summary>
        [ProtoMember(8)]
        public long Volume { get; set; }

        /// <summary>
        /// Weighted average price during the time covered by the bar.
        /// </summary>
        [ProtoMember(9)]
        public double Wap { get; set; }

    }
}
