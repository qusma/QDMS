// -----------------------------------------------------------------------
// <copyright file="ApiResponse.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace QDMSClient
{
    public class ApiResponse
    {
        public bool WasSuccessful { get; protected set; }
        public List<string> Errors { get; protected set; }
        public HttpStatusCode StatusCode { get; protected set; }

        public ApiResponse(HttpResponseMessage response, string error = null)
        {
            WasSuccessful = response.IsSuccessStatusCode;
            StatusCode = response.StatusCode;
            Errors = new List<string>();
            if (error != null) Errors.Add(error);
        }

        public ApiResponse(HttpResponseMessage response, List<string> errors)
        {
            WasSuccessful = response.IsSuccessStatusCode;
            StatusCode = response.StatusCode;
            Errors = errors;
        }

        /// <summary>
        /// When the result is not successful
        /// </summary>
        /// <param name="error"></param>
        public ApiResponse(string error)
        {
            WasSuccessful = false;
            Errors = new List<string> { error };
        }

        public ApiResponse()
        {
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T Result { get; }

        /// <summary>
        /// When the result is not successful
        /// </summary>
        /// <param name="response"></param>
        public ApiResponse(ApiResponse response)
        {
            WasSuccessful = response.WasSuccessful;
            StatusCode = response.StatusCode;
            Errors = response.Errors;
        }

        /// <summary>
        /// When the result is not successful
        /// </summary>
        /// <param name="error"></param>
        public ApiResponse(string error)
        {
            WasSuccessful = false;
            Errors = new List<string> { error };
        }

        /// <summary>
        /// When the request is successful
        /// </summary>
        /// <param name="result"></param>
        public ApiResponse(T result)
        {
            Result = result;
            WasSuccessful = true;
            StatusCode = HttpStatusCode.OK;
        }
    }
}