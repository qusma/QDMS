// -----------------------------------------------------------------------
// <copyright file="ApiClient.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Flurl;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QDMSClient
{
    /// <summary>
    /// Wraps around HttpClient with various useful functions for consuming a REST API
    /// </summary>
    internal class ApiClient : IDisposable
    {
        private HttpClient _httpClient = new HttpClient();

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
            {PreserveReferencesHandling = PreserveReferencesHandling.Objects};
        private readonly string _baseAddr;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="host"></param>
        /// <param name="httpPort"></param>
        /// <param name="apiKey"></param>
        /// <param name="useSsl"></param>
        public ApiClient(string host, int httpPort, string apiKey, bool useSsl)
        {
            _baseAddr = (useSsl ? "https" : "http") + $"://{host}:{httpPort}";
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(apiKey);
            }
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //this allows the use of self-signed certificates
            ServicePointManager
                .ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }

        private async Task<ApiResponse<T>> MakeRequest<T>(Func<HttpClient, Task<HttpResponseMessage>> request) where T : class
        {
            try
            {
                var response = await request(_httpClient).ConfigureAwait(false);
                return await ValidateResponse<T>(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                return HandleClientException<T>(ex);
            }
            catch (TaskCanceledException)
            {
                return new ApiResponse<T>("Request timed out");
            }
        }

        private async Task<ApiResponse> MakeRequest(Func<HttpClient, Task<HttpResponseMessage>> request)
        {
            try
            {
                var response = await request(_httpClient).ConfigureAwait(false);
                return await ValidateResponse(response).ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                return HandleClientException(ex);
            }
            catch (TaskCanceledException)
            {
                return new ApiResponse("Request timed out");
            }
        }

        /// <summary>
        /// GET request
        /// </summary>
        /// <param name="path"></param>
        /// <param name="message">Can't serialize nested objects for GET requests</param>
        /// <typeparam name="T">Type of the object expected in response</typeparam>
        public async Task<ApiResponse<T>> GetAsync<T>(string path, object message = null) where T : class
        {
            var url = GetUrl(path);

            //It's a GET, so if there's an object to send we do it through the query params
            if (message != null)
            {
                url.SetQueryParams(message.GetSerializedPropertyValues());
            }

            return await MakeRequest<T>(x => x.GetAsync(url)).ConfigureAwait(false);
        }

        /// <summary>
        /// POST request
        /// </summary>
        /// <typeparam name="T">Type of the object expected in response</typeparam>
        public async Task<ApiResponse<T>> PostAsync<T>(string path, object body) where T : class
        {
            var url = GetUrl(path);

            StringContent postBody = GetBody(body);
            return await MakeRequest<T>(x => x.PostAsync(url, postBody)).ConfigureAwait(false);
        }

        /// <summary>
        /// PUT request without an expected response object
        /// </summary>
        public async Task<ApiResponse> PutAsync(string path, object body)
        {
            var url = GetUrl(path);
            StringContent postBody = GetBody(body);
            return await MakeRequest(x => x.PutAsync(url, postBody)).ConfigureAwait(false);
        }

        /// <summary>
        /// PUT request
        /// </summary>
        /// <typeparam name="T">Type of the object expected in response</typeparam>
        public async Task<ApiResponse<T>> PutAsync<T>(string path, object body) where T : class
        {
            var url = GetUrl(path);
            StringContent postBody = GetBody(body);
            return await MakeRequest<T>(x => x.PutAsync(url, postBody)).ConfigureAwait(false);
        }

        /// <summary>
        /// Construct a DELETE request with body.
        /// </summary>
        private HttpRequestMessage GetDeleteRequest(string path, object body = null)
        {
            var url = GetUrl(path);

            StringContent postBody = GetBody(body);

            var request = new HttpRequestMessage
            {
                Content = postBody,
                Method = HttpMethod.Delete,
                RequestUri = new Uri(url)
            };

            return request;
        }

        /// <summary>
        /// DELETE request without an expected response object
        /// </summary>
        public async Task<ApiResponse> DeleteAsync(string path, object body = null)
        {
            var request = GetDeleteRequest(path, body);
            return await MakeRequest(x => x.SendAsync(request)).ConfigureAwait(false);
        }

        /// <summary>
        /// DELETE request
        /// </summary>
        /// <typeparam name="T">Type of the object expected in response</typeparam>
        public async Task<ApiResponse<T>> DeleteAsync<T>(string path, object body = null) where T : class
        {
            var request = GetDeleteRequest(path, body);
            return await MakeRequest<T>(x => x.SendAsync(request)).ConfigureAwait(false);
        }

        private Url GetUrl(string path)
        {
            var url = new Url(_baseAddr);
            url.AppendPathSegment(path);
            return url;
        }

        /// <summary>
        /// Serializes an object to JSON and returns it as a StringContent
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        private static StringContent GetBody(object body)
        {
            return body == null
                ? null
                : new StringContent(JsonConvert.SerializeObject(body, SerializerSettings), Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Assuming the request is successful, parses the given type from JSON.
        /// Only call after ValidateResponse
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <exception cref="JsonException"></exception>
        /// <returns></returns>
        private async Task<T> ParseResponse<T>(HttpResponseMessage response) where T : class
        {
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException("Expected response message but body was empty");
            }

            return JsonConvert.DeserializeObject<T>(content);
        }

        private async Task<ApiResponse<T>> ValidateResponse<T>(HttpResponseMessage response) where T : class
        {
            var apiResponse = await ValidateResponse(response).ConfigureAwait(false);
            if (!apiResponse.WasSuccessful)
            {
                return new ApiResponse<T>(apiResponse);
            }

            T result = await ParseResponse<T>(response).ConfigureAwait(false);
            return new ApiResponse<T>(result);
        }

        /// <summary>
        /// Will throw an appropriate exception if the request is not successful
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private async Task<ApiResponse> ValidateResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode) return new ApiResponse(response);

            //Failure! Throw the right exception.
            switch (response.StatusCode)
            {
                case HttpStatusCode.Forbidden:
                    return new ApiResponse(response, "Invalid API key");

                case HttpStatusCode.BadRequest:
                    return await ParseBadRequestResponse(response).ConfigureAwait(false);

                case HttpStatusCode.NotFound:
                case HttpStatusCode.Conflict:
                //nothing special
                default:
                    return await ParseErrorResponse(response).ConfigureAwait(false);
            }
        }

        private static async Task<ApiResponse> ParseErrorResponse(HttpResponseMessage response)
        {
            //Other errors are accompanied by a body that looks like this:
            //{
            //    "StatusCode": 404,
            //    "Message": "The resource you have requested cannot be found.",
            //    "Details": ""
            //}
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(content)) return new ApiResponse(response, "HTTP response error");
            try
            {
                var parsedJson = JObject.Parse(content);
                return new ApiResponse(response, parsedJson.Value<string>("Message"));
            }
            catch (JsonException ex)
            {
                return new ApiResponse(response, $"Failed to parse error message: {content}\n{ex.Message}");
            }
        }

        private static async Task<ApiResponse> ParseBadRequestResponse(HttpResponseMessage response)
        {
            //bad request will normally be followed by JSON explaining why it was a bad request
            //   {
            //       "ErrorMessages": [
            //   		"Name: Name must have a value",
            //   		"Timezone: Timezone does not exist"
            //       ]
            //   }
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(content)) return new ApiResponse(response, "Bad request");

            try
            {
                var parsedJson = JObject.Parse(content);
                var errors = parsedJson["ErrorMessages"].ToObject<List<string>>();
                return new ApiResponse(response, errors);
            }
            catch (JsonException ex)
            {
                return new ApiResponse(response, $"Failed to parse error message: {content}\n{ex.Message}");
            }
        }

        private static ApiResponse<T> HandleClientException<T>(HttpRequestException ex) where T : class
        {
            // The message of the HttpRequestException is typically not very descriptive.
            // But the InnerException can be a WebException which explains exactly what went wrong.
            // e.g. if it can't connect it'll tell you "Unable to connect to the remote server"
            string exMessage = ex.InnerException?.Message ?? ex.Message;
            return new ApiResponse<T>(exMessage);
        }

        private static ApiResponse HandleClientException(HttpRequestException ex)
        {
            string exMessage = ex.InnerException?.Message ?? ex.Message;
            return new ApiResponse(exMessage);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }
    }
}