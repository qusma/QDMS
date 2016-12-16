// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseBroker.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
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
    public interface IEconomicReleaseBroker
    {
        event EventHandler<ErrorArgs> Error;

        ObservableDictionary<string, IEconomicReleaseSource> DataSources { get; }

        Task<List<EconomicRelease>> RequestEconomicReleases(EconomicReleaseRequest request);
    }

    public class EconomicReleaseBroker : IEconomicReleaseBroker
    {
        private readonly string _defaultDataSource;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<ErrorArgs> Error;

        public ObservableDictionary<string, IEconomicReleaseSource> DataSources { get; }

        public EconomicReleaseBroker(string defaultDataSource, IEnumerable<IEconomicReleaseSource> sources)
        {
            _defaultDataSource = defaultDataSource;
            DataSources = new ObservableDictionary<string, IEconomicReleaseSource>();
            if (sources != null)
            {
                foreach (IEconomicReleaseSource ds in sources)
                {
                    DataSources.Add(ds.Name, ds);
                    ds.Error += DatasourceError;
                }
            }

            TryConnect();
        }

        public async Task<List<EconomicRelease>> RequestEconomicReleases(EconomicReleaseRequest request)
        {
            _logger.Info($"ERB: filling request from {request.FromDate:yyyyMMdd} to {request.ToDate:yyyyMMdd} from {request.DataSource ?? "default"} ({request.DataLocation})");

            if (request.DataLocation == DataLocation.LocalOnly)
            {
                return await FillLocalRequest(request).ConfigureAwait(false);
            }

            //What if it's DataLocation.Both? Doesn't really make sense to grab half and half
            //old data is updated with the "actual" value of the release, so we just re-grab everything externally

            //get data externally
            return await FillExternalRequest(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Forward the request to the appropriate external data source
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<List<EconomicRelease>> FillExternalRequest(EconomicReleaseRequest request)
        {
            var client = GetClient(request);

            if (client == null)
            {
                _logger.Error($"ERB: Could not find specified data source {request.DataSource}");
                RaiseEvent(Error, this, new ErrorArgs(-1, $"ERB: Could not find specified data source {request.DataSource}"));
                return new List<EconomicRelease>();
            }

            var data = await client.RequestData(request.FromDate, request.ToDate).ConfigureAwait(false);

            //save the data we got
            try
            {
                using (var context = new MyDBContext())
                {
                    var dbSet = context.Set<EconomicRelease>();
                    foreach (var release in data)
                    {
                        //the data we get might be a duplicate and we want the latest values of everything, so we can't just insert
                        dbSet.AddOrUpdate(x => new { x.Name, x.Country, x.DateTime }, release);
                    }
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ERB: Could not save data");
            }

            //Filter the data if necessary
            if (request.Filter != null)
            {
                data = data.AsQueryable().Where(request.Filter).ToList();
            }

            _logger.Info($"ERB returning {data?.Count} items from {client.Name}");

            return data;
        }

        /// <summary>
        /// Returns the appropriate external datasource for the give request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private IEconomicReleaseSource GetClient(EconomicReleaseRequest request)
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
        /// Return data from the local database
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<List<EconomicRelease>> FillLocalRequest(EconomicReleaseRequest request)
        {
            using (var context = new MyDBContext())
            {
                //this wrangling is necessary because MySql doesn't support TruncateTime()
                //if a time is set, use it as the limit; if not, we want all events from that day
                var toDate = request.ToDate.TimeOfDay.TotalSeconds == 0
                    ? request.ToDate.AddDays(1)
                    : request.ToDate;

                var queryableData = context.EconomicReleases
                    .Where(x =>
                        x.DateTime >= request.FromDate &&
                        x.DateTime <= toDate);

                if (request.Filter != null)
                {
                    queryableData = queryableData.Where(request.Filter);
                }

                try
                {
                    var result = await queryableData.ToListAsync().ConfigureAwait(false);
                    _logger.Info($"ERB returning {result.Count} items from the local db");
                    return result;
                }
                catch (Exception ex)
                {
                    //A malformed filter can cause an exception when querying the db
                    _logger.Error(ex, "ERB: error when querying database - " + ex.Message);
                    return new List<EconomicRelease>();
                }
            }
        }

        /// <summary>
        /// Loops through data sources and tries to connect to those that are disconnected
        /// </summary>
        private void TryConnect()
        {
            foreach (KeyValuePair<string, IEconomicReleaseSource> s in DataSources)
            {
                if (!s.Value.Connected)
                {
                    _logger.Info($"ERB: Trying to connect to data source {s.Key}");
                    s.Value.Connect();
                }
            }
        }

        private void DatasourceError(object sender, ErrorArgs e)
        {
            RaiseEvent(Error, sender, new ErrorArgs(-1, "ERB Client Error: " + e.ErrorMessage, e.RequestID));
            _logger.Error($"ERB: {e.ErrorCode} - {e.ErrorMessage}");
        }

        ///<summary>
        /// Raise the event in a threadsafe manner
        ///</summary>
        static private void RaiseEvent<T>(EventHandler<T> @event, object sender, T e)
        where T : EventArgs
        {
            EventHandler<T> handler = @event;
            handler?.Invoke(sender, e);
        }
    }
}
