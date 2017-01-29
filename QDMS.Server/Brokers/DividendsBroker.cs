// -----------------------------------------------------------------------
// <copyright file="DividendsBroker.cs" company="">
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
    public interface IDividendsBroker
    {
        event EventHandler<ErrorArgs> Error;

        ObservableDictionary<string, IDividendDataSource> DataSources { get; }

        Task<List<Dividend>> RequestDividends(DividendRequest request);
    }

    public class DividendsBroker : IDividendsBroker
    {
        private readonly string _defaultDataSource;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public event EventHandler<ErrorArgs> Error;

        public DividendsBroker(string defaultDataSource, IEnumerable<IDividendDataSource> dataSources)
        {
            _defaultDataSource = defaultDataSource;
            DataSources = new ObservableDictionary<string, IDividendDataSource>();
            if (dataSources != null)
            {
                foreach (IDividendDataSource ds in dataSources)
                {
                    DataSources.Add(ds.Name, ds);
                    ds.Error += DatasourceError;
                }
            }

            TryConnect();
        }

        public ObservableDictionary<string, IDividendDataSource> DataSources { get; }

        public async Task<List<Dividend>> RequestDividends(DividendRequest request)
        {
            _logger.Info($"DivB: filling request from {request.FromDate:yyyyMMdd} to {request.ToDate:yyyyMMdd} {request.Symbol} from {request.DataSource ?? "default"} ({request.DataLocation})");

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
            RaiseEvent(Error, sender, new ErrorArgs(-1, "DivB Client Error: " + e.ErrorMessage, e.RequestID));
            _logger.Error($"DivB: {e.ErrorCode} - {e.ErrorMessage}");
        }

        private async Task<List<Dividend>> FillExternalRequest(DividendRequest request)
        {
            var client = GetClient(request);

            if (client == null)
            {
                _logger.Error($"DivB: Could not find specified data source {request.DataSource}");
                RaiseEvent(Error, this, new ErrorArgs(-1, $"DivB: Could not find specified data source {request.DataSource}"));
                return new List<Dividend>();
            }

            var data = await client.RequestData(request).ConfigureAwait(false);

            //save the data we got
            try
            {
                using (var context = new MyDBContext())
                {
                    var dbSet = context.Set<Dividend>();
                    foreach (var dividend in data)
                    {
                        //the data we get might be a duplicate and we want the latest values of everything, so we can't just insert
                        dbSet.AddOrUpdate(x => new { x.ExDate, x.Symbol }, dividend);
                    }
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DivB: Could not save data");
            }

            _logger.Info($"DivB returning {data?.Count} items from {client.Name}");

            return data;
        }

        private async Task<List<Dividend>> FillLocalRequest(DividendRequest request)
        {
            using (var context = new MyDBContext())
            {
                var queryableData = context.Dividends
                    .Where(x =>
                        x.ExDate >= request.FromDate &&
                        x.ExDate <= request.ToDate);

                if (!string.IsNullOrEmpty(request.Symbol))
                {
                    queryableData = queryableData.Where(x => x.Symbol == request.Symbol);
                }

                try
                {
                    var result = await queryableData.ToListAsync().ConfigureAwait(false);
                    _logger.Info($"DivB returning {result.Count} items from the local db");
                    return result;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "DivB: error when querying database - " + ex.Message);
                    return new List<Dividend>();
                }
            }
        }

        /// <summary>
        /// Returns the appropriate external datasource for the give request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private IDividendDataSource GetClient(DividendRequest request)
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
            foreach (KeyValuePair<string, IDividendDataSource> s in DataSources)
            {
                if (!s.Value.Connected)
                {
                    _logger.Info($"DivB: Trying to connect to data source {s.Key}");
                    s.Value.Connect();
                }
            }
        }
    }
}