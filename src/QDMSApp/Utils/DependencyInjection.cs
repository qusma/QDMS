using EntityData;
using EntityData.Utils;
using MahApps.Metro.Controls.Dialogs;
using Nancy.Bootstrapper;
using QDMS;
using QDMS.Server;
using QDMS.Server.Brokers;
using QDMS.Server.DataSources;
using QDMS.Server.DataSources.Binance;
using QDMS.Server.DataSources.Nasdaq;
using QDMS.Server.Nancy;
using QDMSApp.DataSources;
using QDMSApp.Properties;
using QDMSApp.ViewModels;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using SimpleInjector;
using System;
using System.Linq;

namespace QDMSApp.Utils
{
    internal static class DependencyInjection
    {
        internal static Container ComposeObjects()
        {
            var container = new Container();
            container.Options.SuppressLifestyleMismatchVerification = true;

            container.RegisterSingleton<ISettings>(() => Settings.Default);
            container.Register<IMyDbContext, MyDBContext>(Lifestyle.Transient);
            container.Register<IInstrumentSource, InstrumentRepository>(Lifestyle.Transient);
            container.RegisterSingleton<IDataClient>(() => new QDMSClient.QDMSClient(
                "SERVERCLIENT",
                "127.0.0.1",
                Settings.Default.rtDBReqPort,
                Settings.Default.rtDBPubPort,
                Settings.Default.hDBPort,
                Settings.Default.httpPort,
                Settings.Default.apiKey,
                useSsl: Settings.Default.useSsl));

            container.Register(DataStorageFactory.Get);
            container.Register<ICountryCodeHelper, CountryCodeHelper>(Lifestyle.Singleton);

            //These sources provide both real time and historical data
            var ibRtReg = Lifestyle.Singleton.CreateRegistration<IB>(() => new IB(Settings.Default, new QDMSIBClient.Client(), Settings.Default.rtdClientIBID), container);
            var ibHdReg = Lifestyle.Singleton.CreateRegistration<IB>(() => new IB(Settings.Default, new QDMSIBClient.Client(), Settings.Default.histClientIBID), container);
            var binanceReg = Lifestyle.Singleton.CreateRegistration<Binance>(container);

            var bothSources = new[] { binanceReg };

            //Realtime sources
            var realtimeSources = new Type[]
            {

            };

            container.Collection.Register<IRealTimeDataSource>(realtimeSources
                .Select(type => Lifestyle.Singleton.CreateRegistration(type, container))
                .Concat(bothSources)
                .Concat(new[] { ibRtReg }));


            //Historical sources
            var historicalSources = new[]
            {
                typeof(Yahoo),
                typeof(FRED),
                typeof(Quandl),
                typeof(BarChart)
            };

            container.Collection.Register<IHistoricalDataSource>(historicalSources
                .Select(type => Lifestyle.Singleton.CreateRegistration(type, container))
                .Concat(bothSources)
                .Concat(new[] { ibHdReg }));

            //economic release sources
            var econReleaseSources = new[]
            {
                typeof(fx.FXStreet)
            };

            container.Collection.Register<IEconomicReleaseSource>(econReleaseSources
                .Select(type => Lifestyle.Singleton.CreateRegistration(type, container)));

            //dividend sources
            var dividendSources = new[]
            {
                typeof(NasdaqDs.Nasdaq)
            };

            container.Collection.Register<IDividendDataSource>(dividendSources
                .Select(type => Lifestyle.Singleton.CreateRegistration(type, container)));

            //earnings announcement sources
            var earningsSources = new[]
            {
                typeof(CBOEModule.CBOE)
            };

            container.Collection.Register<IEarningsAnnouncementSource>(earningsSources
                .Select(type => Lifestyle.Singleton.CreateRegistration(type, container)));

            //brokers
            container.Register<IContinuousFuturesBroker, ContinuousFuturesBroker>(Lifestyle.Singleton);
            container.Register<IRealTimeDataBroker, RealTimeDataBroker>(Lifestyle.Singleton);
            container.Register<IHistoricalDataBroker, HistoricalDataBroker>(Lifestyle.Singleton);
            container.Register<IEconomicReleaseBroker, EconomicReleaseBroker>(Lifestyle.Singleton);
            container.Register<IDividendsBroker, DividendsBroker>(Lifestyle.Singleton);
            container.Register<IEarningsAnnouncementBroker, EarningsAnnouncementBroker>(Lifestyle.Singleton);

            //servers
            container.Register<IRealTimeDataServer, RealTimeDataServer>(Lifestyle.Singleton);
            container.Register<IHistoricalDataServer, HistoricalDataServer>(Lifestyle.Singleton);

            //scheduler
            container.Register<IJobFactory, JobFactory>(Lifestyle.Singleton);

            var quartzSettings = QuartzUtils.GetQuartzSettings(Settings.Default.databaseType);
            var factory = new StdSchedulerFactory(quartzSettings);
            container.RegisterSingleton(() => factory.GetScheduler());
            container.RegisterInitializer<IScheduler>(scheduler =>
            {
                scheduler.JobFactory = container.GetInstance<IJobFactory>();
            });

            //http server
            container.Register<INancyBootstrapper, CustomBootstrapper>();

            //UI
            container.Register(() => DialogCoordinator.Instance);

            //ViewModels
            container.Register<MainViewModel>();

            //UI
            container.Register<MainWindow>();

            return container;
        }
    }
}
