// -----------------------------------------------------------------------
// <copyright file="ErrorHandlingMiddleware.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NLog;
using System;
using System.Net;
using System.Threading.Tasks;

namespace QDMS.Server.Http
{
    /// <summary>
    /// Logs requests and turns exceptions into JSON error responses:
    /// bad input -> 400, unique constraint violations -> 409, anything else -> 500.
    /// </summary>
    public static class ErrorHandlingMiddleware
    {
        private static readonly Logger Logger = LogManager.GetLogger("QDMS.Server.Http");

        private static bool IsQuiet(HttpRequest request) =>
            request.Path == "/" ||
            request.Path.StartsWithSegments("/ui") ||
            (HttpMethods.IsGet(request.Method) &&
             (request.Path.StartsWithSegments("/api/logs") ||
              request.Path.StartsWithSegments("/datasources/status") ||
              request.Path.StartsWithSegments("/datasources/activestreams")));

        public static IApplicationBuilder UseQdmsErrorHandling(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                //the web UI's files and its status polling would drown out everything else
                Logger.Log(IsQuiet(context.Request) ? LogLevel.Debug : LogLevel.Info,
                    $"http server request {context.Request.Method} {context.Request.Path}{context.Request.QueryString}");

                try
                {
                    await next(context).ConfigureAwait(false);
                }
                catch (Exception ex) when (!context.Response.HasStarted)
                {
                    await HandleException(context, ex).ConfigureAwait(false);
                }
            });
        }

        private static Task HandleException(HttpContext context, Exception ex)
        {
            Logger.Error(ex, "Unhandled exception in http server: " + ex.Message);

            IResult result;
            if (ex is BadHttpRequestException || ex is ArgumentException)
            {
                result = HttpApi.ValidationFailure(new[] { ex.Message });
            }
            else if (ex.IsUniqueKeyException())
            {
                result = HttpApi.Error(HttpStatusCode.Conflict, ex.GetBaseException().Message);
            }
            else
            {
                result = HttpApi.Error(HttpStatusCode.InternalServerError, ex.Message);
            }

            return result.ExecuteAsync(context);
        }
    }
}
