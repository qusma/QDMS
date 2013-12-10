// -----------------------------------------------------------------------
// <copyright file="ContinuousFuturesRolloverType.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    [Serializable]
    public enum ContinuousFuturesRolloverType
    {
        /// <summary>
        /// Time
        /// </summary>
        [Description("Time")]
        Time = 0,
        /// <summary>
        /// Volume
        /// </summary>
        [Description("Volume")]
        Volume = 1,
        /// <summary>
        /// Open Interest
        /// </summary>
        [Description("Open Interest")]
        OpenInterest = 2,
        /// <summary>
        /// Volume And Open Interest
        /// </summary>
        [Description("Volume And Open Interest")]
        VolumeAndOpenInterest = 3,
        /// <summary>
        /// Volume Or Open Interest
        /// </summary>
        [Description("Volume Or Open Interest")]
        VolumeOrOpenInterest = 4,
    }
}
