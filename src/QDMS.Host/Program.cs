// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using QDMS;
using QDMS.Host;
using QDMS.Server;
using QDMS.Server.Brokers;
using QDMS.Server.DataSources;
using QDMS.Server.DataSources.Binance;
using QDMS.Server.DataSources.Tiingo;
using QDMS.Server.DataStorage;
using QDMS.Server.Http;
using QDMS.Server.Jobs;
using QDMS.Server.Repositories;
using QDMS.Server.Services;
using Quartz;
using System;
using System.Linq;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var settings = builder.Configuration.GetSection("Qdms").Get<QdmsSettings>() ?? new QdmsSettings();
    if (string.IsNullOrEmpty(settings.apiKey))
        throw new InvalidOperationException("Qdms:apiKey must be set (e.g. with the Qdms__apiKey environment variable)");

    string metadataConnection = builder.Configuration.GetConnectionString("Qdms")
        ?? throw new InvalidOperationException("ConnectionStrings:Qdms must be set");
    string dataConnection = builder.Configuration.GetConnectionString("QdmsData") ?? metadataConnection;

    builder.WebHost.UseUrls($"http://0.0.0.0:{settings.httpPort}");

    ConfigureServices(builder.Services, settings, metadataConnection, dataConnection);

    var app = builder.Build();

    InitializeDatabase(app.Services);

    app.UseQdmsErrorHandling();
    app.MapQdmsApi(settings.apiKey);

    app.Run();
}
catch (Exception ex)
{
    logger.Fatal(ex, "QDMS stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}

static void ConfigureServices(IServiceCollection services, QdmsSettings settings, string metadataConnection, string dataConnection)
{
    services.AddSingleton(settings);
    services.AddSingleton<ISettings>(settings);

    //databases
    services.AddDbContextFactory<MyDBContext>(o => DbSetup.ConfigureMetadataDb(o, metadataConnection));
    services.AddScoped<IMyDbContext>(sp => sp.GetRequiredService<MyDBContext>());
    services.AddDbContextFactory<DataDBContext>(o => DbSetup.ConfigureDataDb(o, dataConnection));
    services.AddSingleton<IDataStorage>(_ => new PostgresStorage(dataConnection));
    services.AddScoped<IInstrumentSource, InstrumentRepository>();

    //the continuous futures broker gets its data through a client connected to this server
    services.AddSingleton<IDataClient>(_ => new QDMSClient.QDMSClient(
        "SERVERCLIENT",
        "127.0.0.1",
        settings.rtDBReqPort,
        settings.rtDBPubPort,
        settings.hDBPort,
        settings.httpPort,
        settings.apiKey));

    //data sources
    services.AddSingleton<Binance>();
    services.AddSingleton<FRED>();
    services.AddSingleton<IHistoricalDataSource>(sp => sp.GetRequiredService<Binance>());
    services.AddSingleton<IHistoricalDataSource>(sp => sp.GetRequiredService<FRED>());
    services.AddSingleton<IHistoricalDataSource>(_ => new Tiingo(settings)); //stays disconnected without tiingoApiKey
    services.AddSingleton<IRealTimeDataSource>(sp => sp.GetRequiredService<Binance>());
    if (settings.enableInteractiveBrokers)
    {
        services.AddSingleton<IHistoricalDataSource>(_ => new IB(settings, new QDMSIBClient.Client(), settings.histClientIBID));
        services.AddSingleton<IRealTimeDataSource>(_ => new IB(settings, new QDMSIBClient.Client(), settings.rtdClientIBID));
    }

    //brokers
    services.AddSingleton<IContinuousFuturesBroker>(sp => new ContinuousFuturesBroker(
        sp.GetRequiredService<IDataClient>(),
        new InstrumentRepository(sp.GetRequiredService<IDbContextFactory<MyDBContext>>().CreateDbContext())));
    services.AddSingleton<IHistoricalDataBroker>(sp => new HistoricalDataBroker(
        sp.GetRequiredService<IContinuousFuturesBroker>(),
        sp.GetRequiredService<IDataStorage>(),
        sp.GetServices<IHistoricalDataSource>()));
    services.AddSingleton<IRealTimeDataBroker>(sp => new RealTimeDataBroker(
        sp.GetRequiredService<IContinuousFuturesBroker>(),
        sp.GetRequiredService<IDataStorage>(),
        sp.GetServices<IRealTimeDataSource>()));
    services.AddSingleton<IEconomicReleaseBroker, EconomicReleaseBroker>();
    services.AddSingleton<IDividendsBroker, DividendsBroker>();
    services.AddSingleton<IEarningsAnnouncementBroker, EarningsAnnouncementBroker>();

    //ZeroMQ servers
    services.AddSingleton<IHistoricalDataServer, HistoricalDataServer>();
    services.AddSingleton<IRealTimeDataServer, RealTimeDataServer>();
    services.AddHostedService<ServerHostedService>();

    //scheduler: update jobs live in Quartz's tables in the metadata database
    //(Quartz creates its qrtz_* tables on first start)
    services.AddQuartz(q =>
    {
        q.ConfigureScheduler(o =>
        {
            o.InstanceName = "QdmsScheduler";
            o.InstanceId = "instance_one";
        });
        q.UseJobFactory<JobFactory>();
        q.UsePersistentStore(s =>
        {
            s.UsePostgres(metadataConnection);
            s.UseSystemTextJsonSerializer();
            s.ConfigureStore(o =>
            {
                o.StoreJobDataAsStrings = true;
                o.TablePrefix = "qrtz_";
            });
            s.ProvisionSchema();
        });
    });
    services.AddQuartzHostedService(o =>
    {
        o.WaitForJobsToComplete = true;
        o.StartDelay = TimeSpan.FromSeconds(10);
    });
    services.AddSingleton(sp => sp.GetRequiredService<ISchedulerFactory>().GetScheduler().GetAwaiter().GetResult());
    services.AddScoped<IJobsRepository, JobsRepository>();

    //HTTP API
    services.AddScoped<IDatasourceService, DatasourceService>();
    services.AddQdmsApiValidators();
}

static void InitializeDatabase(IServiceProvider services)
{
    var log = LogManager.GetLogger("QDMS.Host.Startup");

    using (var dataContext = services.GetRequiredService<IDbContextFactory<DataDBContext>>().CreateDbContext())
    {
        dataContext.Database.Migrate();
    }

    using var context = services.GetRequiredService<IDbContextFactory<MyDBContext>>().CreateDbContext();
    context.Database.Migrate();

    //datasources are seeded no matter what, because new ones get added
    Seed.SeedDatasources(context);

    if (!context.Exchanges.Any())
    {
        log.Info("Empty database, seeding exchanges, sessions, countries and currencies");
        Seed.DoSeed(context);
    }
}
