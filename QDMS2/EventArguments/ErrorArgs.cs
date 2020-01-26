// -----------------------------------------------------------------------
// <copyright file="ErrorArgs.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace QDMS
{
    /// <summary>
    /// 
    /// </summary>
    public class ErrorArgs : EventArgs
    {
        /// <summary>
        /// Event args for error event.
        /// </summary>
        public ErrorArgs(int code, string message, int? requestID = null)
        {
            ErrorCode = code;
            ErrorMessage = message;
            RequestID = requestID;
        }

        /// <summary>
        /// Error code associated with this error.
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// If this error concerns a specific request, its RequestID is given here.
        /// </summary>
        public int? RequestID { get; set; }
    }
}
