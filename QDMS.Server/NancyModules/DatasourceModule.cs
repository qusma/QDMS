// -----------------------------------------------------------------------
// <copyright file="DatasourceModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Nancy;
using Nancy.Security;
using QDMS.Server.Brokers;
using QDMSServer;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace QDMS.Server.NancyModules
{
    public class DatasourceModule : NancyModule
    {
        public DatasourceModule(IMyDbContext context, IRealTimeDataBroker rtb, IHistoricalDataBroker hdb, IEconomicReleaseBroker erb)
            : base("/datasources")
        {
            this.RequiresAuthentication();

            Get["/", runAsync: true] = async (_, token) => await context.Datasources.ToListAsync(token).ConfigureAwait(false);

            Get["/status"] = _ =>
            {
                var realtime = rtb.DataSources.Values.ToDictionary(x => x.Name, x => x.Connected);
                var historical = hdb.DataSources.Values.ToDictionary(x => x.Name, x => x.Connected);
                var econReleases = erb.DataSources.Values.ToDictionary(x => x.Name, x => x.Connected);

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
            };

            Get["/activestreams"] = _ => rtb.ActiveStreams;
        }
    }
}