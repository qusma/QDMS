// -----------------------------------------------------------------------
// <copyright file="UnderlyingSymbolEndpoints.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace QDMS.Server.Http
{
    public static class UnderlyingSymbolEndpoints
    {
        public static void MapUnderlyingSymbolEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/underlyingsymbols");

            group.MapGet("/", async (IMyDbContext context) =>
                HttpApi.Json(await context.UnderlyingSymbols.ToListAsync().ConfigureAwait(false)));

            group.MapGet("/{id:int}", async (int id, IMyDbContext context) =>
            {
                var symbol = await context.UnderlyingSymbols.FirstOrDefaultAsync(x => x.ID == id).ConfigureAwait(false);
                return symbol == null ? HttpApi.NotFound() : HttpApi.Json(symbol);
            });

            group.MapPut("/", async (HttpContext http, IMyDbContext context) =>
            {
                var (newValues, failure) = await http.ReadAndValidateBody<UnderlyingSymbol>().ConfigureAwait(false);
                if (failure != null) return failure;

                //make sure the symbol we want to update exists
                var symbol = await context.UnderlyingSymbols.FirstOrDefaultAsync(x => x.ID == newValues.ID).ConfigureAwait(false);
                if (symbol == null) return HttpApi.NotFound();

                context.UpdateEntryValues(symbol, newValues);
                await context.SaveChangesAsync().ConfigureAwait(false);

                return HttpApi.Json(symbol);
            });

            group.MapPost("/", async (HttpContext http, IMyDbContext context) =>
            {
                var (symbol, failure) = await http.ReadAndValidateBody<UnderlyingSymbol>().ConfigureAwait(false);
                if (failure != null) return failure;

                context.UnderlyingSymbols.Add(symbol);
                await context.SaveChangesAsync().ConfigureAwait(false);

                //return the object with the id after inserting
                return HttpApi.Json(symbol);
            });

            group.MapDelete("/{id:int}", async (int id, IMyDbContext context) =>
            {
                var symbol = await context.UnderlyingSymbols.FirstOrDefaultAsync(x => x.ID == id).ConfigureAwait(false);
                if (symbol == null) return HttpApi.NotFound();

                var symbolReferenced = await context.ContinuousFutures.AnyAsync(x => x.UnderlyingSymbolID == id).ConfigureAwait(false);
                if (symbolReferenced)
                {
                    return HttpApi.Error(HttpStatusCode.Conflict, "Can't delete this underlying symbol because it has continuous futures assigned to it.");
                }

                context.UnderlyingSymbols.Remove(symbol);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return HttpApi.Json(symbol);
            });
        }
    }
}
