// -----------------------------------------------------------------------
// <copyright file="DataEndpoints.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using QDMS.Server.Brokers;
using QDMS.Server.Services;
using System.Linq;
using System.Threading;

namespace QDMS.Server.Http
{
    /// <summary>
    /// Datasources, dividends, earnings announcements, economic releases.
    /// </summary>
    public static class DataEndpoints
    {
        public static void MapDatasourceEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/datasources");

            group.MapGet("/", async (IDatasourceService service, CancellationToken token) =>
                HttpApi.Json(await service.GetAll(token).ConfigureAwait(false)));

            group.MapGet("/status", (IDatasourceService service) => HttpApi.Json(service.GetDatasourceStatus()));

            group.MapGet("/activestreams", (IDatasourceService service) => HttpApi.Json(service.GetActiveStreams()));
        }

        public static void MapDividendEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/dividends");

            group.MapGet("/", async (HttpRequest request, IDividendsBroker broker) =>
            {
                var req = request.BindQuery<DividendRequest>();
                return HttpApi.Json(await broker.RequestDividends(req).ConfigureAwait(false));
            });

            group.MapGet("/datasources", (IDividendsBroker broker) => HttpApi.Json(broker.DataSources.Keys.ToList()));
        }

        public static void MapEarningsAnnouncementEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/earningsannouncements");

            group.MapGet("/", async (HttpRequest request, IEarningsAnnouncementBroker broker) =>
            {
                var req = request.BindQuery<EarningsAnnouncementRequest>();
                return HttpApi.Json(await broker.Request(req).ConfigureAwait(false));
            });

            group.MapGet("/datasources", (IEarningsAnnouncementBroker broker) => HttpApi.Json(broker.DataSources.Keys.ToList()));
        }

        public static void MapEconomicReleaseEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/economicreleases");

            group.MapGet("/", async (HttpRequest request, IMyDbContext context, IEconomicReleaseBroker broker, CancellationToken token) =>
            {
                // No request object, just return everything
                if (request.Query.Count == 0)
                {
                    return HttpApi.Json(await context.EconomicReleases.ToListAsync(token).ConfigureAwait(false));
                }

                var req = request.BindQuery<EconomicReleaseRequest>();
                return HttpApi.Json(await broker.RequestEconomicReleases(req).ConfigureAwait(false));
            });

            group.MapGet("/datasources", (IEconomicReleaseBroker broker) => HttpApi.Json(broker.DataSources.Keys.ToList()));
        }
    }
}
