// -----------------------------------------------------------------------
// <copyright file="RealTimeTickEventArgs.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using ProtoBuf;

namespace QDMS
{
    /// <summary>
    ///     Used to surface real-time tick data
    /// </summary>
    [ProtoContract]
    public class RealTimeTickEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        /// <param name="tickType"></param>
        /// <param name="last"></param>
        /// <param name="bid"></param>
        /// <param name="ask"></param>
        /// <param name="lastQuantity"></param>
        /// <param name="bidQuantity"></param>
        /// <param name="askQuantity"></param>
        public RealTimeTickEventArgs(TickType tickType, decimal last = 0, decimal bid = 0, decimal ask = 0,
            decimal lastQuantity = 0, decimal bidQuantity = 0, decimal askQuantity = 0)
        {
            TickType = tickType;
            Last = last;
            Bid = bid;
            Ask = ask;
            LastQuantity = lastQuantity;
            BidQuantity = bidQuantity;
            AskQuantity = askQuantity;
        }

        /// <summary>
        ///     Used for serialization
        /// </summary>
        public RealTimeTickEventArgs()
        {
        }

        /// <summary>
        ///     Event time
        /// </summary>
        [ProtoMember(1)]
        public DateTime DT { get; set; }

        /// <summary>
        ///     Last trade price
        /// </summary>
        [ProtoMember(2)]
        public decimal Last { get; set; }

        /// <summary>
        ///     Bid price at event time
        /// </summary>
        [ProtoMember(3)]
        public decimal Bid { get; set; }

        /// <summary>
        ///     Ask price at event time
        /// </summary>
        [ProtoMember(4)]
        public decimal Ask { get; set; }

        /// <summary>
        ///     Quantity of the last trade
        /// </summary>
        [ProtoMember(5)]
        public decimal LastQuantity { get; set; }

        /// <summary>
        ///     Quantity available at the bid
        /// </summary>
        [ProtoMember(6)]
        public decimal BidQuantity { get; set; }

        /// <summary>
        ///     Quantity available at the ask
        /// </summary>
        [ProtoMember(7)]
        public decimal AskQuantity { get; set; }

        /// <summary>
        ///     Event type
        /// </summary>
        [ProtoMember(8)]
        public TickType TickType { get; set; }

        /// <summary>
        /// True if odd lot
        /// </summary>
        [ProtoMember(9)]
        public bool OddLot { get; set; }

        /// <summary>
        /// Indicates exchange tick origin
        /// </summary>
        [ProtoMember(10)]
        public string ExchangeCode { get; set; }

        /// <summary>
        /// The ID of the instrument.
        /// </summary>
        [ProtoMember(20)]
        public int InstrumentID { get; set; }
    }
}