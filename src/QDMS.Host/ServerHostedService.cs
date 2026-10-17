// -----------------------------------------------------------------------
// <copyright file="ServerHostedService.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using NLog;
using QDMS.Server;
using System.Threading;
using System.Threading.Tasks;

namespace QDMS.Host
{
    /// <summary>
    /// Starts and stops the ZeroMQ servers for historical and real-time data.
    /// (Quartz and the HTTP API have their own hosted services.)
    /// </summary>
    public class ServerHostedService : IHostedService
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IHistoricalDataServer _historicalDataServer;
        private readonly IRealTimeDataServer _realTimeDataServer;
        private readonly IRealTimeDataBroker _realTimeDataBroker;
        private readonly IHistoricalDataBroker _historicalDataBroker;

        public ServerHostedService(
            IHistoricalDataServer historicalDataServer,
            IRealTimeDataServer realTimeDataServer,
            IRealTimeDataBroker realTimeDataBroker,
            IHistoricalDataBroker historicalDataBroker)
        {
            _historicalDataServer = historicalDataServer;
            _realTimeDataServer = realTimeDataServer;
            _realTimeDataBroker = realTimeDataBroker;
            _historicalDataBroker = historicalDataBroker;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _realTimeDataServer.StartServer();
            _historicalDataServer.StartServer();
            _logger.Info("ZeroMQ servers started");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _realTimeDataServer.StopServer();
            _historicalDataServer.StopServer();
            _realTimeDataBroker.Dispose();
            _historicalDataBroker.Dispose();
            _logger.Info("ZeroMQ servers stopped");
            return Task.CompletedTask;
        }
    }
}
