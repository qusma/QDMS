// -----------------------------------------------------------------------
// <copyright file="DataAdditionRequest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    public class DataAdditionRequest
    {
        [ProtoMember(1)]
        public BarSize Frequency;

        [ProtoMember(2)]
        public Instrument Instrument;

        [ProtoMember(3)]
        public List<OHLCBar> Data;

        [ProtoMember(4)]
        public bool Overwrite = true;

        public DataAdditionRequest()
        {
            Data = new List<OHLCBar>();
        }
    }
}
