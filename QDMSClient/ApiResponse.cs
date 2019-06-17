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
    /// <summary>
    /// Holds responses to API requests
    /// </summary>
    public class ApiResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public bool WasSuccessful { get; protected set; }

        /// <summary>
        /// If the request was not successful, errors will be found here
        /// </summary>
        public List<string> Errors { get; protected set; }

        /// <summary>
        /// Status code
        /// </summary>
        public HttpStatusCode StatusCode { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="error"></param>
        public ApiResponse(HttpResponseMessage response, string error = null)
        {
            WasSuccessful = response.IsSuccessStatusCode;
            StatusCode = response.StatusCode;
            Errors = new List<string>();
            if (error != null) Errors.Add(error);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="errors"></param>
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

        /// <summary>
        /// 
        /// </summary>
        public ApiResponse()
        {
        }
    }

    /// <summary>
    /// Holds responses to API requests, including a returned object T
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ApiResponse<T> : ApiResponse
    {
        /// <summary>
        /// 
        /// </summary>
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