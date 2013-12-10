// -----------------------------------------------------------------------
// <copyright file="OptionType.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    [Serializable]
    public enum OptionType
    {
        /// <summary>
        /// Call Option
        /// </summary>
        [Description("Call")]
        Call = 0,
        /// <summary>
        /// Put Option
        /// </summary>
        [Description("Put")]
        Put = 1,
    }
}
