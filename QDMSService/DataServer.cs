using EntityData;
using Nancy.Hosting.Self;
using NLog;
using QDMS;
using QDMS.Server.Brokers;
using QDMS.Server.DataSources;
using QDMS.Server.Nancy;
using QDMSServer;
using System;
using System.Linq;
using QDMS.Server;
using QDMS.Server.DataSources.Nasdaq;
using Quartz;
using Quartz.Impl;

namespace QDMSService
{
    public sealed class DataServer
    {
        private Logger _log;
        private Config.DataService _config;

        private HistoricalDataBroker _historicalDataBroker;
        private RealTimeDataBroker _realTimeDataBroker;
        private EconomicReleaseBroker _economicReleaseBroker;

        private HistoricalDataServer _historicalDataServer;
        private RealTimeDataServer _realTimeDataServer;
        private NancyHost _httpServer;

        private IScheduler _scheduler;
        private IDividendsBroker _dividendBroker;

        public DataServer(Config.DataService config)
        {
            _config = config;
            _log = LogManager.GetCurrentClassLogger();
        }

        public void Initialisize()
        {
            _log.Info($"Server is initialisizing ...");

            //create data db if it doesn't exist
            DataDBContext dataContext;
            try
            {
                dataContext = new DataDBContext(_config.LocalStorage.ConnectionString);
                dataContext.Database.Initialize(false);
            }
            catch (System.Data.Entity.Core.ProviderIncompatibleException ex)
            {
                throw new NotSupportedException("Could not connect to context DataDB!", ex);
            }
            dataContext.Dispose();

            MyDBContext entityContext;
            try
            {
                entityContext = new MyDBContext(_config.DataStorage.ConnectionString);
                entityContext.Database.Initialize(false);
            }
            catch (System.Data.Entity.Core.ProviderIncompatibleException ex)
            {
                throw new NotSupportedException("Could not connect to context MyDB!", ex);
            }

            // initialisize helper classes

            var cfRealtimeBroker = new ContinuousFuturesBroker(GetClient("RTDBCFClient"), false);
            var cfHistoricalBroker = new ContinuousFuturesBroker(GetClient("HDBCFClient"), false);

            IDataStorage localStorage;

            switch (_config.LocalStorage.Type)
            {
                case LocalStorageType.MySql:
                    localStorage = new QDMSServer.DataSources.MySQLStorage(_config.LocalStorage.ConnectionString);
                    break;

                case LocalStorageType.SqlServer:
                    localStorage = new QDMSServer.DataSources.SqlServerStorage(_config.LocalStorage.ConnectionString);
                    break;

                default:
                    throw new NotSupportedException("Not supported local storage type: " + _config.LocalStorage.Type);
            }

            // create brokers
            _historicalDataBroker = new HistoricalDataBroker(cfHistoricalBroker, localStorage, new IHistoricalDataSource[] {
                // @todo please add here some historical data sources the service should provide
            });
            _realTimeDataBroker = new RealTimeDataBroker(cfRealtimeBroker, localStorage, new IRealTimeDataSource[] {
                // @todo please add here some real time data sources the service should provide
            });
            _economicReleaseBroker = new EconomicReleaseBroker("FXStreet",
                new[] { new fx.FXStreet(new CountryCodeHelper(entityContext.Countries.ToList())) });

            _dividendBroker = new DividendsBroker("Nasdaq",
                new[] { new NasdaqDs.Nasdaq() });

            // create servers
            _historicalDataServer = new HistoricalDataServer(_config.HistoricalDataService.Port, _historicalDataBroker);
            _realTimeDataServer = new RealTimeDataServer(_config.RealtimeDataService.PublisherPort, _config.RealtimeDataService.RequestPort, _realTimeDataBroker);

            // create scheduler
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory(); //todo need settings for scheduler + jobfactory
            _scheduler = schedulerFactory.GetScheduler();

            var bootstrapper = new CustomBootstrapper(
                localStorage,
                _economicReleaseBroker,
                _historicalDataBroker,
                _realTimeDataBroker,
                _dividendBroker,
                _scheduler,
               _config.WebService.ApiKey);
            var uri = new Uri((_config.WebService.UseSsl ? "https" : "http") + "://localhost:" + _config.WebService.Port);
            _httpServer = new NancyHost(bootstrapper, uri);

            // ... start the servers
            _historicalDataServer.StartServer();
            _realTimeDataServer.StartServer();
            _httpServer.Start();
            _scheduler.Start();

            _log.Info($"Server is ready.");
        }

        private QDMSClient.QDMSClient GetClient(string clientName)
        {
            return new QDMSClient.QDMSClient(
                clientName,
                "127.0.0.1",
                _config.RealtimeDataService.RequestPort,
                _config.RealtimeDataService.PublisherPort,
                _config.HistoricalDataService.Port,
                _config.WebService.Port,
                _config.WebService.ApiKey,
                _config.WebService.UseSsl);
        }

        public void Stop()
        {
            _realTimeDataServer.Dispose();
            _historicalDataBroker.Dispose();
            _httpServer.Dispose();
            _scheduler.Shutdown();
        }
    }
}