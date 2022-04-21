// -----------------------------------------------------------------------
// <copyright file="ErrorResponse.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Nancy;

namespace QDMS.Server.Nancy
{
    internal class ErrorResponse
    {
        public ErrorResponse()
        {
        }

        public ErrorResponse(HttpStatusCode statusCode, string message, string details)
        {
            StatusCode = statusCode;
            Message = message;
            Details = details;
        }

        public HttpStatusCode StatusCode { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }
}