// -----------------------------------------------------------------------
// <copyright file="Importance.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// How important an economic announcement is expected to be
    /// </summary>
    [Serializable]
    public enum Importance
    {
        /// <summary>
        /// None
        /// </summary>
        [Description("None")]
        None = 0,
        /// <summary>
        /// Low expected impact
        /// </summary>
        [Description("Low expected impact")]
        Low = 1,
        /// <summary>
        /// Mid expected impact
        /// </summary>
        [Description("Mid expected impact")]
        Mid = 2,
        /// <summary>
        /// High expected impact
        /// </summary>
        [Description("High expected impact")]
        High = 3
    }
}
