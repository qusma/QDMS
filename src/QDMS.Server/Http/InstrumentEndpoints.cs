// -----------------------------------------------------------------------
// <copyright file="InstrumentEndpoints.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace QDMS.Server.Http
{
    public static class InstrumentEndpoints
    {
        public static void MapInstrumentEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/instruments");

            group.MapGet("/", async (IInstrumentSource repo) =>
                HttpApi.Json(await repo.FindInstruments().ConfigureAwait(false)));

            group.MapGet("/{id:int}", async (int id, IInstrumentSource repo) =>
            {
                var instrument = (await repo.FindInstruments(x => x.ID == id).ConfigureAwait(false)).FirstOrDefault();
                return instrument == null ? HttpApi.NotFound() : HttpApi.Json(instrument);
            });

            group.MapGet("/{id:int}/storageinfo", async (int id, IInstrumentSource repo, IDataStorage dataStorage) =>
            {
                var instrument = (await repo.FindInstruments(x => x.ID == id).ConfigureAwait(false)).FirstOrDefault();
                if (instrument == null) return HttpApi.NotFound();

                var storageInfo = dataStorage.GetStorageInfo(id);
                return storageInfo == null ? HttpApi.NotFound() : HttpApi.Json(storageInfo);
            });

            //Search using an instrument object
            group.MapGet("/search", async (HttpRequest request, IInstrumentSource repo) =>
            {
                var search = request.BindQuery<Instrument>();
                return HttpApi.Json(await repo.FindInstruments(search).ConfigureAwait(false));
            });

            //Search using a serialized LINQ predicate
            group.MapGet("/predsearch", async (HttpRequest request, IInstrumentSource repo) =>
            {
                var predReq = request.BindQuery<PredicateSearchRequest>();

                Expression<Func<Instrument, bool>> expression;
                try
                {
                    expression = predReq.Filter;
                }
                catch (Exception ex)
                {
                    return HttpApi.ValidationFailure(new[] { "Malformed predicate: " + ex.Message });
                }

                if (expression == null) return HttpApi.ValidationFailure(new[] { "Missing predicate" });

                return HttpApi.Json(await repo.FindInstruments(expression).ConfigureAwait(false));
            });

            group.MapPost("/", async (HttpContext context, IInstrumentSource repo) =>
            {
                var (instrument, failure) = await context.ReadAndValidateBody<Instrument>().ConfigureAwait(false);
                if (failure != null) return failure;

                return HttpApi.Json(await repo.AddInstrument(instrument).ConfigureAwait(false));
            });

            group.MapPut("/", async (HttpContext context, IInstrumentSource repo) =>
            {
                var (instrument, failure) = await context.ReadAndValidateBody<Instrument>().ConfigureAwait(false);
                if (failure != null) return failure;

                var instrumentFromDb = (await repo.FindInstruments(x => x.ID == instrument.ID).ConfigureAwait(false)).FirstOrDefault();
                if (instrumentFromDb == null) return HttpApi.NotFound();

                await repo.UpdateInstrument(instrumentFromDb, instrument).ConfigureAwait(false);

                return HttpApi.Json(instrumentFromDb);
            });

            group.MapDelete("/{id:int}", async (int id, IInstrumentSource repo, IDataStorage dataStorage) =>
            {
                var instrument = (await repo.FindInstruments(x => x.ID == id).ConfigureAwait(false)).FirstOrDefault();
                if (instrument == null) return HttpApi.NotFound();

                await repo.RemoveInstrument(instrument, dataStorage).ConfigureAwait(false);
                return HttpApi.Json(instrument);
            });
        }
    }
}
