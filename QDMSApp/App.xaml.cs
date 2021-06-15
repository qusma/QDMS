using Common.Logging.NLog;
using EntityData;
using EntityData.Utils;
using MahApps.Metro.Controls.Dialogs;
using Nancy.Bootstrapper;
using NLog;
using NLog.Targets;
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
using QDMSIBClient;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using SimpleInjector;
using System;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Windows;

namespace QDMSApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Common.Logging.LogManager.Adapter = new NLogLoggerFactoryAdapter(new Common.Logging.Configuration.NameValueCollection());

            //Prompt for db settings if not found, we need them before we can do anything
            CheckDBConnection();

            //set the log directory
            SetLogDirectory();

            //Log unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;

            //set the connection string
            DBUtils.SetConnectionString();

            //set EF configuration, necessary for MySql to work
            DBUtils.SetDbConfiguration();

            //database creation/migration
            CreateDatabases();

            ComposeObjects();
        }

        private void CreateDatabases()
        {
            //create metadata db if it doesn't exist
            var entityContext = new MyDBContext();
            entityContext.Database.Initialize(false);

            //seed the datasources no matter what, because these are added frequently
            Seed.SeedDatasources(entityContext);

            //check for any exchanges, seed the db with initial values if nothing is found
            if (!entityContext.Exchanges.Any() ||
                (ApplicationDeployment.IsNetworkDeployed && ApplicationDeployment.CurrentDeployment.IsFirstRun))
            {
                Seed.DoSeed();
            }

            entityContext.Dispose();

            //create data db if it doesn't exist
            var dataContext = new DataDBContext();
            dataContext.Database.Initialize(false);
            dataContext.Dispose();

            //create quartz db if it doesn't exist
            QuartzUtils.InitializeDatabase(Settings.Default.databaseType);
        }

        private void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Error((Exception)e.ExceptionObject, "Unhandled exception");
        }

        private void SetLogDirectory()
        {
            if (Directory.Exists(QDMSApp.Properties.Settings.Default.logDirectory))
            {
                ((FileTarget)LogManager.Configuration.FindTargetByName("default")).FileName = QDMSApp.Properties.Settings.Default.logDirectory + "Log.log";
            }
        }

        private void CheckDBConnection()
        {
            //if no db type has been selected, we gotta show that window no matter what
            if (Settings.Default.databaseType != "MySql" && Settings.Default.databaseType != "SqlServer")
            {
                var dbDetailsWindow = new DBConnectionWindow();
                dbDetailsWindow.ShowDialog();
            }

            if (!DBUtils.TestConnection())
            {
                var dbDetailsWindow = new DBConnectionWindow();
                dbDetailsWindow.ShowDialog();
            }
        }

        private void ComposeObjects()
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
            var ibReg = Lifestyle.Singleton.CreateRegistration<IB>(container);
            var binanceReg = Lifestyle.Singleton.CreateRegistration<Binance>(container);

            var bothSources = new[] { ibReg, binanceReg };

            //Realtime sources
            container.Register<IIBClient, Client>();
            var realtimeSources = new Type[]
            {

            };

            container.Collection.Register<IRealTimeDataSource>(realtimeSources
                .Select(type => Lifestyle.Singleton.CreateRegistration(type, container))
                .Concat(bothSources));


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
                .Concat(bothSources));

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
            var vm = container.GetInstance<MainViewModel>();
            var mainWindow = new MainWindow(vm, container.GetInstance<IDataClient>());
            mainWindow.Show();
        }
    }
}
