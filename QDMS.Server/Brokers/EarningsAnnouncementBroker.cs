// -----------------------------------------------------------------------
// <copyright file="EarningsAnnouncementBroker.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using NLog;
using QDMSServer;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Threading.Tasks;

namespace QDMS.Server.Brokers
{
    public interface IEarningsAnnouncementBroker
    {
        event EventHandler<ErrorArgs> Error;

        ObservableDictionary<string, IEarningsAnnouncementSource> DataSources { get; }

        Task<List<EarningsAnnouncement>> Request(EarningsAnnouncementRequest request);
    }

    public class EarningsAnnouncementBroker : IEarningsAnnouncementBroker
    {
        private readonly string _defaultDataSource;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<ErrorArgs> Error;

        public EarningsAnnouncementBroker(IEnumerable<IEarningsAnnouncementSource> dataSources)
        {
            _defaultDataSource = "CBOE";
            DataSources = new ObservableDictionary<string, IEarningsAnnouncementSource>();
            if (dataSources != null)
            {
                foreach (IEarningsAnnouncementSource ds in dataSources)
                {
                    DataSources.Add(ds.Name, ds);
                    ds.Error += DatasourceError;
                }
            }

            TryConnect();
        }

        public ObservableDictionary<string, IEarningsAnnouncementSource> DataSources { get; }

        public async Task<List<EarningsAnnouncement>> Request(EarningsAnnouncementRequest request)
        {
            string symbols = request.Symbol == null
                ? ""
                : string.Join(", ", request.Symbol);
            _logger.Info($"EAB: filling request from {request.FromDate:yyyyMMdd} to {request.ToDate:yyyyMMdd} {symbols} from {request.DataSource ?? "default"} ({request.DataLocation})");

            if (request.DataLocation == DataLocation.LocalOnly)
            {
                return await FillLocalRequest(request).ConfigureAwait(false);
            }

            //What if it's DataLocation.Both? Doesn't really make sense to grab half and half
            //old data is updated with the "actual" value of the release, so we just re-grab everything externally

            //get data externally
            return await FillExternalRequest(request).ConfigureAwait(false);
        }

        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        private static void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            EventHandler<T> handler = @event;
            handler?.Invoke(sender, e);
        }

        private void DatasourceError(object sender, ErrorArgs e)
        {
            RaiseEvent(Error, sender, new ErrorArgs(-1, "EAB Client Error: " + e.ErrorMessage, e.RequestID));
            _logger.Error($"EAB: {e.ErrorCode} - {e.ErrorMessage}");
        }

        private async Task<List<EarningsAnnouncement>> FillExternalRequest(EarningsAnnouncementRequest request)
        {
            var client = GetClient(request);

            if (client == null)
            {
                _logger.Error($"EAB: Could not find specified data source {request.DataSource}");
                RaiseEvent(Error, this, new ErrorArgs(-1, $"EAB: Could not find specified data source {request.DataSource}"));
                throw new Exception("Could not find specified data source {request.DataSource}");
            }

            var data = await client.RequestData(request).ConfigureAwait(false);

            //save the data we got
            try
            {
                using (var context = new MyDBContext())
                {
                    var dbSet = context.Set<EarningsAnnouncement>();
                    foreach (var ea in data)
                    {
                        //the data we get might be a duplicate and we want the latest values of everything, so we can't just insert
                        dbSet.AddOrUpdate(x => new { x.Date, x.Symbol }, ea);
                    }
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "EAB: Could not save data");
            }

            _logger.Info($"EAB returning {data?.Count} items from {client.Name}");

            return data;
        }

        private async Task<List<EarningsAnnouncement>> FillLocalRequest(EarningsAnnouncementRequest request)
        {
            using (var context = new MyDBContext())
            {
                var queryableData = context.EarningsAnnouncements
                    .Where(x =>
                        x.Date >= request.FromDate &&
                        x.Date <= request.ToDate);//TODO we want end of day as cutoff

                if (request.Symbol != null && request.Symbol.Count > 0)
                {
                    queryableData = queryableData.BuildContainsExpression(request.Symbol, x => x.Symbol);
                }

                try
                {
                    var result = await queryableData.OrderBy(x => x.Date).ToListAsync().ConfigureAwait(false);
                    _logger.Info($"EAB returning {result.Count} items from the local db");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "EAB: error when querying database - " + ex.Message);
                    return new List<EarningsAnnouncement>();
                }
            }
        }

        /// <summary>
        /// Returns the appropriate external datasource for the give request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private IEarningsAnnouncementSource GetClient(EarningsAnnouncementRequest request)
        {
            if (!string.IsNullOrEmpty(request.DataSource))
            {
                return DataSources.ContainsKey(request.DataSource) ? DataSources[request.DataSource] : null;
            }
            else
            {
                return DataSources[_defaultDataSource];
            }
        }

        /// <summary>
        /// Loops through data sources and tries to connect to those that are disconnected
        /// </summary>
        private void TryConnect()
        {
            foreach (KeyValuePair<string, IEarningsAnnouncementSource> s in DataSources)
            {
                if (!s.Value.Connected)
                {
                    _logger.Info($"EAB: Trying to connect to data source {s.Key}");
                    s.Value.Connect();
                }
            }
        }
    }
}