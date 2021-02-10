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
        private EarningsAnnouncementBroker _earningsAnnouncementBroker;

        public DataServer(Config.DataService config)
        {
            _config = config;
            _log = LogManager.GetCurrentClassLogger();
        }

        public void Initialize()
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
                entityContext = new MyDBContext();
                entityContext.Database.Initialize(false);
            }
            catch (System.Data.Entity.Core.ProviderIncompatibleException ex)
            {
                throw new NotSupportedException("Could not connect to context MyDB!", ex);
            }

            _log.Info($"Server is ready.");
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