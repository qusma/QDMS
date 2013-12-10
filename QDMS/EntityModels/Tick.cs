// -----------------------------------------------------------------------
// <copyright file="Tick.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using ProtoBuf;

namespace QDMS
{
    [Serializable]
    [ProtoContract]
    public class Tick
    {
        [ProtoMember(1)]
        public DateTime DT { get; set; }

        [ProtoMember(2)]
        public decimal Price { get; set; }

        [ProtoMember(3)]
        public int Contracts { get; set; }
    }
}
