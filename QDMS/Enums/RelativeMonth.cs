// -----------------------------------------------------------------------
// <copyright file="RelativeMonth.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    [Serializable]
    public enum RelativeMonth : int
    {
        /// <summary>
        /// Previous Month
        /// </summary>
        [Description("Previous Month")]
        PreviousMonth = -1,
        /// <summary>
        /// Current Month
        /// </summary>
        [Description("Current Month")]
        CurrentMonth = 0,
        /// <summary>
        /// Next Month
        /// </summary>
        [Description("Next Month")]
        NextMonth = 1
    }
}
