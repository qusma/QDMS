﻿// -----------------------------------------------------------------------
// <copyright file="CustomBootstrapper.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using MySql.Data.MySqlClient;
using Nancy;
using Nancy.Authentication.Stateless;
using Nancy.Bootstrapper;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Serialization.JsonNet;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using NLog;
using QDMS.Server.Brokers;
using QDMS.Server.Repositories;
using QDMS.Server.Services;
using QDMSApp;
using Quartz;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;

namespace QDMS.Server.Nancy
{
    public class CustomBootstrapper : DefaultNancyBootstrapper
    {
        private readonly IDataStorage _storage;
        private readonly IEconomicReleaseBroker _erb;
        private readonly IHistoricalDataBroker _hdb;
        private readonly IRealTimeDataBroker _rtdb;
        private readonly IDividendsBroker _divb;
        private readonly IEarningsAnnouncementBroker _eab;
        private readonly IScheduler _scheduler;
        private readonly string _apiKey;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public CustomBootstrapper(
            IDataStorage storage,
            IEconomicReleaseBroker erb,
            IHistoricalDataBroker hdb,
            IRealTimeDataBroker rtdb,
            IDividendsBroker divb,
            IEarningsAnnouncementBroker eab,
            IScheduler scheduler,
            ISettings settings)
        {
            _storage = storage;
            _erb = erb;
            _hdb = hdb;
            _rtdb = rtdb;
            _divb = divb;
            _eab = eab;
            _scheduler = scheduler;
            _apiKey = settings.apiKey;
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);

            //configure ioc
            container.Register<IMyDbContext, MyDBContext>().AsMultiInstance();
            container.Register<IInstrumentSource, InstrumentRepository>().AsMultiInstance();
            container.Register<IJobsRepository, JobsRepository>().AsMultiInstance();
            container.Register<IScheduler>(_scheduler);
            container.Register<IDataStorage>(_storage);
            container.Register<IEconomicReleaseBroker>(_erb);
            container.Register<IHistoricalDataBroker>(_hdb);
            container.Register<IRealTimeDataBroker>(_rtdb);
            container.Register<IDividendsBroker>(_divb);
            container.Register<IEarningsAnnouncementBroker>(_eab);
            container.Register<JsonSerializer, CustomJsonSerializer>();

            container.Register<IDatasourceService, DatasourceService>();
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            //log requests
            pipelines.BeforeRequest += ctx =>
            {
                _logger.Info($"http server request {ctx.Request.Method} {ctx.Request.Url}");
                return null;
            };

            pipelines.OnError.AddItemToEndOfPipeline((ctx, ex) =>
            {
                //log errors
                _logger.Error(ex, "Unhandled exception in http server: " + ex.Message);

                //special response for model binding exceptions
                if (ex is ModelBindingException)
                {
                    var response = new JsonResponse(new ValidationErrorResponse(ex.Message), new JsonNetSerializer(), ctx.Environment);
                    response.StatusCode = HttpStatusCode.BadRequest;
                    return response;
                }

                //handle situations where an operation would violate a unique constraint in the db - return 409 Conflict
                var sqlException = ex.GetBaseException() as SqlException;
                if (sqlException != null && sqlException.IsUniqueKeyException())
                {
                    var response = new JsonResponse(
                        new ErrorResponse(HttpStatusCode.Conflict, sqlException.Message, ""),
                        new JsonNetSerializer(),
                        ctx.Environment);
                    response.StatusCode = HttpStatusCode.Conflict;
                    return response;
                }

                var mysqlException = ex.GetBaseException() as MySqlException;
                if (mysqlException != null && mysqlException.IsUniqueKeyException())
                {
                    var response = new JsonResponse(
                        new ErrorResponse(HttpStatusCode.Conflict, mysqlException.Message, ""),
                        new JsonNetSerializer(),
                        ctx.Environment);
                    response.StatusCode = HttpStatusCode.Conflict;
                    return response;
                }

                //generic handler
                var genericResponse = new JsonResponse(
                    new ErrorResponse(HttpStatusCode.InternalServerError, ex.Message, ""),
                    new JsonNetSerializer(),
                    ctx.Environment);
                genericResponse.StatusCode = HttpStatusCode.InternalServerError;
                return genericResponse;
            });

            var statelessAuthConfiguration = new StatelessAuthenticationConfiguration(ctx =>
            {
                var authHeader = ctx.Request.Headers["Authorization"];
                if (authHeader == null || !authHeader.Any())
                {
                    return null;
                }

                if (authHeader.First() == _apiKey)
                {
                    return new System.Security.Claims.ClaimsPrincipal(new GenericIdentity("admin"));
                }

                return null;
            });

            //Enables authentication for all modules
            StatelessAuthentication.Enable(pipelines, statelessAuthConfiguration);
        }
    }
}