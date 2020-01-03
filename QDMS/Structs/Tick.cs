// -----------------------------------------------------------------------
// <copyright file="Tick.cs" company="">
// Copyright 2019 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using ProtoBuf;

namespace QDMS
{
    /// <summary>
    /// Represents one tick
    /// </summary>
    [ProtoContract]
    public struct Tick
    {
        /// <summary>
        ///     Event time
        /// </summary>
        [ProtoMember(1)] public long Time;

        /// <summary>
        ///     Last trade price
        /// </summary>
        [ProtoMember(2)] public double Last;

        /// <summary>
        ///     Bid price at event time
        /// </summary>
        [ProtoMember(3)] public double Bid;

        /// <summary>
        ///     Ask price at event time
        /// </summary>
        [ProtoMember(4)] public double Ask;

        /// <summary>
        ///     Quantity of the last trade
        /// </summary>
        [ProtoMember(5)] public double LastQuantity;

        /// <summary>
        ///     Quantity available at the bid
        /// </summary>
        [ProtoMember(6)] public double BidQuantity;

        /// <summary>
        ///     Quantity available at the ask
        /// </summary>
        [ProtoMember(7)] public double AskQuantity;

        /// <summary>
        ///     Event type
        /// </summary>
        [ProtoMember(8)] public TickType TickType;

        /// <summary>
        ///     True if odd lot
        /// </summary>
        [ProtoMember(9)] public bool OddLot;

        /// <summary>
        ///     Indicates exchange tick origin
        /// </summary>
        [ProtoMember(10)] public string ExchangeCode;

        /// <summary>
        ///     Symbol
        /// </summary>
        [ProtoMember(11)] public string Symbol;

        /// <summary>
        ///     The ID of the instrument.
        /// </summary>
        [ProtoMember(12)] public int InstrumentID;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="time"></param>
        /// <param name="tickType"></param>
        /// <param name="symbol"></param>
        /// <param name="instrumentId"></param>
        /// <param name="last"></param>
        /// <param name="bid"></param>
        /// <param name="ask"></param>
        /// <param name="lastQuantity"></param>
        /// <param name="bidQuantity"></param>
        /// <param name="askQuantity"></param>
        /// <param name="oddLot"></param>
        /// <param name="exchangeCode"></param>
        public Tick(long time, TickType tickType, string symbol, int instrumentId, double last = 0, double bid = 0, double ask = 0, double lastQuantity = 0, double bidQuantity = 0, double askQuantity = 0, bool oddLot = false, string exchangeCode = null)
        {
            Time = time;
            Last = last;
            Bid = bid;
            Ask = ask;
            LastQuantity = lastQuantity;
            BidQuantity = bidQuantity;
            AskQuantity = askQuantity;
            TickType = tickType;
            OddLot = oddLot;
            ExchangeCode = exchangeCode;
            Symbol = symbol;
            InstrumentID = instrumentId;
        }
    }
}