// -----------------------------------------------------------------------
// <copyright file="HttpApi.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QDMS.Server.Http
{
    /// <summary>
    /// Plumbing for the HTTP API. The wire format matches what the old Nancy server produced, so that
    /// existing QDMSClient versions keep working:
    /// - JSON via Newtonsoft with PreserveReferencesHandling.Objects, PascalCase names
    /// - GET parameters as flat query strings (see QDMSClient's GetSerializedPropertyValues)
    /// - errors as {StatusCode, Message, Details}, validation failures as 400 {ErrorMessages: [...]}
    /// - the API key as the raw value of the Authorization header
    /// </summary>
    public static class HttpApi
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        /// <summary>
        /// Map all API endpoints. Every endpoint requires the API key.
        /// </summary>
        public static RouteGroupBuilder MapQdmsApi(this IEndpointRouteBuilder app, string apiKey)
        {
            var api = app.MapGroup("");
            api.AddEndpointFilter(new ApiKeyFilter(apiKey));

            api.MapInstrumentEndpoints();
            api.MapExchangeEndpoints();
            api.MapSessionTemplateEndpoints();
            api.MapTagEndpoints();
            api.MapUnderlyingSymbolEndpoints();
            api.MapDatasourceEndpoints();
            api.MapJobEndpoints();
            api.MapDividendEndpoints();
            api.MapEarningsAnnouncementEndpoints();
            api.MapEconomicReleaseEndpoints();
            api.MapWebUiEndpoints();

            return api;
        }

        /// <summary>
        /// Adds the FluentValidation validators used by the API.
        /// </summary>
        public static IServiceCollection AddQdmsApiValidators(this IServiceCollection services)
        {
            var validatorTypes = typeof(HttpApi).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } &&
                            t.BaseType.GetGenericTypeDefinition() == typeof(AbstractValidator<>));
            foreach (var type in validatorTypes)
            {
                services.AddSingleton(typeof(IValidator<>).MakeGenericType(type.BaseType.GetGenericArguments()[0]), type);
            }

            return services;
        }

        public static IResult Json(object value, int statusCode = StatusCodes.Status200OK) =>
            new NewtonsoftJsonResult(value, statusCode);

        public static IResult Error(HttpStatusCode statusCode, string message) =>
            Json(new ErrorResponse(statusCode, message, ""), (int)statusCode);

        public static IResult NotFound() =>
            Error(HttpStatusCode.NotFound, "The resource you have requested cannot be found.");

        public static IResult ValidationFailure(IEnumerable<string> errors) =>
            Json(new ValidationErrorResponse { ErrorMessages = errors.ToList() }, StatusCodes.Status400BadRequest);

        /// <summary>
        /// Deserialize the request body.
        /// </summary>
        /// <exception cref="BadHttpRequestException">The body is missing or not valid JSON.</exception>
        public static async Task<T> ReadBody<T>(this HttpRequest request)
        {
            using var reader = new StreamReader(request.Body, Encoding.UTF8);
            string body = await reader.ReadToEndAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body))
                throw new BadHttpRequestException("Request body is empty");

            try
            {
                return JsonConvert.DeserializeObject<T>(body, SerializerSettings);
            }
            catch (JsonException ex)
            {
                throw new BadHttpRequestException("Could not parse request body: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Deserialize and validate the request body. Returns null and sets <paramref name="failure"/> if invalid.
        /// </summary>
        public static async Task<(T Value, IResult Failure)> ReadAndValidateBody<T>(this HttpContext context)
        {
            T value = await context.Request.ReadBody<T>().ConfigureAwait(false);
            var validator = context.RequestServices.GetService<IValidator<T>>();
            if (validator == null) return (value, null);

            var result = await validator.ValidateAsync(value).ConfigureAwait(false);
            if (result.IsValid) return (value, null);

            return (value, ValidationFailure(result.Errors.Select(x => $"{x.PropertyName}: {x.ErrorMessage}")));
        }

        /// <summary>
        /// Bind an object from the query string: each simple public property is set from the
        /// matching (case-insensitive) query parameter, if present.
        /// </summary>
        public static T BindQuery<T>(this HttpRequest request) where T : new()
        {
            var obj = new T();
            foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite || prop.GetIndexParameters().Length > 0) continue;
                if (!request.Query.TryGetValue(prop.Name, out var values)) continue;

                string value = values.ToString();
                var converter = TypeDescriptor.GetConverter(prop.PropertyType);
                if (!converter.CanConvertFrom(typeof(string))) continue;

                try
                {
                    prop.SetValue(obj, converter.ConvertFromInvariantString(value));
                }
                catch (Exception ex)
                {
                    throw new BadHttpRequestException($"Invalid value for {prop.Name}: {value}", ex);
                }
            }

            return obj;
        }

        private sealed class NewtonsoftJsonResult : IResult
        {
            private readonly object _value;
            private readonly int _statusCode;

            public NewtonsoftJsonResult(object value, int statusCode)
            {
                _value = value;
                _statusCode = statusCode;
            }

            public Task ExecuteAsync(HttpContext httpContext)
            {
                httpContext.Response.StatusCode = _statusCode;
                httpContext.Response.ContentType = "application/json; charset=utf-8";
                return httpContext.Response.WriteAsync(JsonConvert.SerializeObject(_value, SerializerSettings));
            }
        }

        private sealed class ApiKeyFilter : IEndpointFilter
        {
            private readonly string _apiKey;

            public ApiKeyFilter(string apiKey)
            {
                _apiKey = apiKey;
            }

            public ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
            {
                string header = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
                if (string.IsNullOrEmpty(_apiKey) || header != _apiKey)
                {
                    return new ValueTask<object>(Error(HttpStatusCode.Unauthorized, "Invalid API key"));
                }

                return next(context);
            }
        }
    }

    public class ErrorResponse
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

    public class ValidationErrorResponse
    {
        public List<string> ErrorMessages { get; set; }
    }
}
