// -----------------------------------------------------------------------
// <copyright file="RealTimeDataEventArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using ProtoBuf;

namespace QDMS
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [ProtoContract]
    public class RealTimeDataEventArgs : EventArgs
    {
        /// <summary>
        /// Real time bar event arguments.
        /// </summary>
        /// <param name="instrumentID"></param>
        /// <param name="frequency"></param>
        /// <param name="time">The time at the opening of the bar.</param>
        /// <param name="open"></param>
        /// <param name="high"></param>
        /// <param name="low"></param>
        /// <param name="close"></param>
        /// <param name="volume"></param>
        /// <param name="wap"></param>
        /// <param name="count"></param>
        /// <param name="requestID"></param>
        public RealTimeDataEventArgs(int instrumentID, BarSize frequency, long time, decimal open, decimal high, decimal low, decimal close, long volume, double wap, int count, int requestID)
        {
            InstrumentID = instrumentID;
            Frequency = frequency;
            Time = time;
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            Wap = wap;
            Count = count;
            RequestID = requestID;
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
        /// The ID of the instrument.
        /// </summary>
        [ProtoMember(6)]
        public int InstrumentID { get; set; }

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

        /// <summary>
        /// The AssignedID of the original RealTimeDataRequest that started this stream.
        /// </summary>
        public int RequestID { get; set; }
        public BarSize Frequency { get; }
    }
}
