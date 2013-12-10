// -----------------------------------------------------------------------
// <copyright file="ErrorArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    public class ErrorArgs : EventArgs
    {
        public ErrorArgs(int code, string message)
        {
            ErrorCode = code;
            ErrorMessage = message;
        }

        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }
}
