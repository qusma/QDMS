// -----------------------------------------------------------------------
// <copyright file="Tick.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    public class Tick
    {
        public Tick(decimal last = 0, decimal bid = 0, decimal ask = 0, int lastQuantity = 0, int bidQuantity = 0, int askQuantity = 0)
        {
            Last = last;
            Bid = bid;
            Ask = ask;
            LastQuantity = lastQuantity;
            BidQuantity = bidQuantity;
            AskQuantity = askQuantity;
        }

        [ProtoMember(1)]
        public DateTime DT { get; set; }

        [ProtoMember(2)]
        public decimal Last { get; set; }

        [ProtoMember(3)]
        public decimal Bid { get; set; }

        [ProtoMember(4)]
        public decimal Ask { get; set; }

        /// <summary>
        /// Quantity of the last trade
        /// </summary>
        [ProtoMember(5)]
        public int LastQuantity { get; set; }

        [ProtoMember(6)]
        public int BidQuantity { get; set; }

        [ProtoMember(7)]
        public int AskQuantity { get; set; }

        /// <summary>
        /// For serialization
        /// </summary>
        public Tick()
        {
        }


    }
}
