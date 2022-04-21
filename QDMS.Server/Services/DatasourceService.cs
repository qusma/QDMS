using EntityData;
using Microsoft.EntityFrameworkCore;
using QDMS.Server.Brokers;
using QDMSApp;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QDMS.Server.Services
{
    public class DatasourceService : IDatasourceService
    {
        private readonly IMyDbContext _context;
        private readonly IRealTimeDataBroker _rtb;
        private readonly IHistoricalDataBroker _hdb;
        private readonly IEconomicReleaseBroker _erb;

        public DatasourceService(IMyDbContext context, IRealTimeDataBroker rtb, IHistoricalDataBroker hdb, IEconomicReleaseBroker erb)
        {
            _context = context;
            _rtb = rtb;
            _hdb = hdb;
            _erb = erb;
        }

        public async Task<List<Datasource>> GetAll(CancellationToken token)
        {
            return await _context.Datasources.ToListAsync(token).ConfigureAwait(false);
        }

        public List<DataSourceStatus> GetDatasourceStatus()
        {
            var realtime = _rtb.DataSources.Values.ToDictionary(x => x.Name, x => x.Connected);
            var historical = _hdb.DataSources.Values.ToDictionary(x => x.Name, x => x.Connected);
            var econReleases = _erb.DataSources.Values.ToDictionary(x => x.Name, x => x.Connected);

            var names = realtime.Keys.Union(historical.Keys).Union(econReleases.Keys);

            var statuses = new List<DataSourceStatus>();
            foreach (var name in names)
            {
                var status = new DataSourceStatus
                {
                    Name = name,
                    RealtimeConnected = realtime.ContainsKey(name) ? realtime[name] : (bool?)null,
                    HistoricalConnected = historical.ContainsKey(name) ? historical[name] : (bool?)null,
                    EconReleasesConnected = econReleases.ContainsKey(name) ? econReleases[name] : (bool?)null,
                };
                statuses.Add(status);
            }

            return statuses;
        }

        public ConcurrentNotifierBlockingList<RealTimeStreamInfo> GetActiveStreams()
        {
            return _rtb.ActiveStreams;
        }
    }
}
