// -----------------------------------------------------------------------
// <copyright file="SessionsSource.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    [Serializable]
    public enum SessionsSource : int
    {
        /// <summary>
        /// Exchange
        /// </summary>
        [Description("Exchange")]
        Exchange = 0,
        /// <summary>
        /// Template
        /// </summary>
        [Description("Template")]
        Template = 1,
        /// <summary>
        /// Custom
        /// </summary>
        [Description("Custom")]
        Custom = 2,
    }
}
