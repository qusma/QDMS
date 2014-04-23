// -----------------------------------------------------------------------
// <copyright file="ColumnOptions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMSServer
{
    /// <summary>
    /// Holds options for a DataGrid Column.
    /// </summary>
    [Serializable]
    public class ColumnOptions
    {
        public int DisplayIndex { get; set; }
        public double Width { get; set; }
        public ListSortDirection? SortDirection { get; set; }
    }
}
