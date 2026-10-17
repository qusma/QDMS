// -----------------------------------------------------------------------
// <copyright file="ExchangeEndpoints.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;

namespace QDMS.Server.Http
{
    public static class ExchangeEndpoints
    {
        public static void MapExchangeEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/exchanges");

            group.MapGet("/", async (IMyDbContext context) =>
                HttpApi.Json(await context.Exchanges.Include(x => x.Sessions).ToListAsync().ConfigureAwait(false)));

            group.MapGet("/{id:int}", async (int id, IMyDbContext context) =>
            {
                var exchange = await context.Exchanges.Include(x => x.Sessions).FirstOrDefaultAsync(x => x.ID == id).ConfigureAwait(false);
                return exchange == null ? HttpApi.NotFound() : HttpApi.Json(exchange);
            });

            group.MapPost("/", async (HttpContext http, IMyDbContext context) =>
            {
                var (exchange, failure) = await http.ReadAndValidateBody<Exchange>().ConfigureAwait(false);
                if (failure != null) return failure;

                context.Exchanges.Add(exchange);
                await context.SaveChangesAsync().ConfigureAwait(false);

                //return the object with the id after inserting
                return HttpApi.Json(exchange);
            });

            group.MapPut("/", async (HttpContext http, IMyDbContext context) =>
            {
                var (newValues, failure) = await http.ReadAndValidateBody<Exchange>().ConfigureAwait(false);
                if (failure != null) return failure;

                //make sure the exchange we want to update exists
                var exchange = await context.Exchanges.Include(x => x.Sessions).FirstOrDefaultAsync(x => x.ID == newValues.ID).ConfigureAwait(false);
                if (exchange == null) return HttpApi.NotFound();

                //update values on the exchange
                context.UpdateEntryValues(exchange, newValues);

                //add/remove/update the Sessions collection
                exchange.Sessions.UpdateCollectionAndElements(newValues.Sessions, context);

                //some instruments may have their sessions based on this exchange, we need to update them
                var instruments = await context.Instruments
                    .Include(x => x.Sessions)
                    .Where(x => x.SessionsSource == SessionsSource.Exchange && x.ExchangeID == exchange.ID)
                    .ToListAsync().ConfigureAwait(false);

                foreach (Instrument i in instruments)
                {
                    context.InstrumentSessions.RemoveRange(i.Sessions);
                    i.Sessions.Clear();

                    foreach (ExchangeSession s in exchange.Sessions)
                    {
                        i.Sessions.Add(s.ToInstrumentSession());
                    }
                }

                await context.SaveChangesAsync().ConfigureAwait(false);

                return HttpApi.Json(exchange);
            });

            group.MapDelete("/{id:int}", async (int id, IMyDbContext context) =>
            {
                var exchange = await context.Exchanges.FirstOrDefaultAsync(x => x.ID == id).ConfigureAwait(false);
                if (exchange == null) return HttpApi.NotFound();

                var exchangeReferenced = await context.Instruments.AnyAsync(x => x.ExchangeID == id || x.PrimaryExchangeID == id).ConfigureAwait(false);
                if (exchangeReferenced)
                {
                    return HttpApi.Error(HttpStatusCode.Conflict, "Can't delete this exchange because it has instruments assigned to it.");
                }

                context.Exchanges.Remove(exchange);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return HttpApi.Json(exchange);
            });
        }
    }
}
