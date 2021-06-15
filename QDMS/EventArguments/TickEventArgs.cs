// -----------------------------------------------------------------------
// <copyright file="TickEventArgs.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using ProtoBuf;
using System;

namespace QDMS
{
    /// <summary>
    ///     Used to surface real-time tick data
    /// </summary>
    [ProtoContract]
    public class TickEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(1)]
        public Tick Tick { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tick"></param>
        public TickEventArgs(Tick tick)
        {
            Tick = tick;
        }

        /// <summary>
        /// For serialization
        /// </summary>
        public TickEventArgs()
        { }
    }
}