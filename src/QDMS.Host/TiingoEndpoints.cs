// -----------------------------------------------------------------------
// <copyright file="TiingoEndpoints.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using QDMS.Server.DataSources.Tiingo;
using QDMS.Server.Http;
using System;
using System.Net;

namespace QDMS.Host
{
    /// <summary>
    /// Ticker search for the web UI's "add instrument from Tiingo". Goes through the server so the API key stays here.
    /// </summary>
    public static class TiingoEndpoints
    {
        public static void MapTiingoEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/tiingo");

            group.MapGet("/search", async (string query, [FromServices] Tiingo tiingo) =>
            {
                if (string.IsNullOrWhiteSpace(query)) return HttpApi.ValidationFailure(new[] { "Missing query" });
                try
                {
                    return HttpApi.Json(await tiingo.Search(query).ConfigureAwait(false));
                }
                catch (InvalidOperationException ex)
                {
                    return HttpApi.Error(HttpStatusCode.BadGateway, ex.Message);
                }
            });

            group.MapGet("/meta/{ticker}", async (string ticker, [FromServices] Tiingo tiingo) =>
            {
                try
                {
                    return HttpApi.Json(await tiingo.GetMeta(ticker).ConfigureAwait(false));
                }
                catch (InvalidOperationException ex)
                {
                    return HttpApi.Error(HttpStatusCode.BadGateway, ex.Message);
                }
            });
        }
    }
}
