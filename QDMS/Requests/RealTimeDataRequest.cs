// -----------------------------------------------------------------------
// <copyright file="RealTimeDataRequest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    public class RealTimeDataRequest
    {
        [ProtoMember(1)]
        public BarSize Frequency { get; set; }

        [ProtoMember(2)]
        public bool RTHOnly { get; set; }

        [ProtoMember(3)]
        public Instrument Instrument { get; set; }

        [ProtoMember(4)]
        public bool SaveToLocalStorage { get; set; }

        public RealTimeDataRequest()
        {
        }

        public RealTimeDataRequest(Instrument instrument, BarSize frequency, bool rthOnly = true, bool savetoLocalStorage = false)
        {
            Instrument = instrument;
            Frequency = frequency;
            RTHOnly = rthOnly;
            SaveToLocalStorage = savetoLocalStorage;
        }
    }
}
