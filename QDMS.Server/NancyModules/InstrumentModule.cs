﻿// -----------------------------------------------------------------------
// <copyright file="InstrumentsModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using QDMS.Server.Nancy;
using QDMSApp;
using System;
using System.Linq;
using System.Linq.Expressions;
using Nancy.Extensions;

namespace QDMS.Server.NancyModules
{
    public class InstrumentModule : NancyModule
    {
        public InstrumentModule(IInstrumentSource instrumentRepo, IDataStorage dataStorage) : base("/instruments")
        {
            this.RequiresAuthentication();

            Get("/", async (_, token) => await instrumentRepo.FindInstruments().ConfigureAwait(false));

            Get("/{Id:int}", async (parameters, token) =>
            {
                //Instrument by ID
                var id = (int)parameters.Id;
                var instrument = (await instrumentRepo.FindInstruments(x => x.ID == id).ConfigureAwait(false)).FirstOrDefault();

                if (instrument == null) return HttpStatusCode.NotFound;

                return instrument;
            });

            Get("/{Id:int}/storageinfo", async (parameters, token) =>
            {
                //storage info by instrument id
                var id = (int)parameters.Id;
                var instrument = (await instrumentRepo.FindInstruments(x => x.ID == id).ConfigureAwait(false)).FirstOrDefault();

                if (instrument == null) return HttpStatusCode.NotFound;

                var storageInfo = dataStorage.GetStorageInfo(id);

                if (storageInfo == null) return HttpStatusCode.NotFound;

                return storageInfo;
            });

            Get("/search", async (_, token) =>
            {
                //Search using an instrument object
                var inst = this.Bind<Instrument>();
                if (inst == null) return HttpStatusCode.BadRequest;

                return await instrumentRepo.FindInstruments(inst).ConfigureAwait(false);
            });

            Get("/predsearch", async (parameters, token) =>
            {
                //Search using a predicate
                var predReq = this.Bind<PredicateSearchRequest>();
                if (predReq == null) return HttpStatusCode.BadRequest;

                //Deserialize LINQ expression and pass it to the instrument manager
                Expression<Func<Instrument, bool>> expression;
                try
                {
                    expression = predReq.Filter;
                }
                catch (Exception ex)
                {
                    return Negotiate
                        .WithModel(new ValidationErrorResponse("Malformed predicate: " + ex.Message))
                        .WithStatusCode(HttpStatusCode.BadRequest);
                }
                var instruments = await instrumentRepo.FindInstruments(expression).ConfigureAwait(false);

                return instruments;
            });

            Post("/", async (parameters, token) =>
            {
                Instrument instrument = this.BindAndValidate<Instrument>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                var addedInstrument = await instrumentRepo.AddInstrument(instrument);
                return addedInstrument;
            });

            Put("/", async (parameters, token) =>
            {
                var instrument = this.BindAndValidate<Instrument>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                //find it
                Instrument instrumentFromDB = (await instrumentRepo.FindInstruments(x => x.ID == instrument.ID).ConfigureAwait(false)).FirstOrDefault();
                if (instrumentFromDB == null) return HttpStatusCode.NotFound;

                //update it
                await instrumentRepo.UpdateInstrument(instrumentFromDB, instrument).ConfigureAwait(false);

                return instrumentFromDB;
            });

            Delete("/{Id:int}", async (parameters, token) =>
            {
                int id = parameters.Id;
                var instrument = (await instrumentRepo.FindInstruments(x => x.ID == id).ConfigureAwait(false)).FirstOrDefault();

                if (instrument == null) return HttpStatusCode.NotFound;

                await instrumentRepo.RemoveInstrument(instrument, dataStorage).ConfigureAwait(false);
                return instrument;
            });
        }
    }
}