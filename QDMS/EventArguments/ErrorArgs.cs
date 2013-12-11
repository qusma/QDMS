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
        /// <summary>
        /// Event args for error event.
        /// </summary>
        public ErrorArgs(int code, string message)
        {
            ErrorCode = code;
            ErrorMessage = message;
        }

        /// <summary>
        /// Error code associated with this error.
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}
