// -----------------------------------------------------------------------
// <copyright file="IDataClient.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMSClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace QDMS
{
    public interface IDataClient : IDisposable, INotifyPropertyChanged
    {
        event EventHandler<ErrorArgs> Error;

        event EventHandler<HistoricalDataEventArgs> HistoricalDataReceived;

        event EventHandler<LocallyAvailableDataInfoReceivedEventArgs> LocallyAvailableDataInfoReceived;

        event EventHandler<RealTimeDataEventArgs> RealTimeDataReceived;
        bool Connected { get; }

        /// <summary>
        /// Keeps track of historical requests that have been sent but the data has not been received yet.
        /// </summary>
        ObservableCollection<HistoricalDataRequest> PendingHistoricalRequests { get; }

        /// <summary>
        /// Keeps track of live real time data streams.
        /// </summary>
        ObservableCollection<RealTimeDataRequest> RealTimeDataStreams { get; }

        /// <summary>
        /// Add a new exchange
        /// </summary>
        Task<ApiResponse<Exchange>> AddExchange(Exchange exchange);

        /// <summary>
        /// Add a new instrument
        /// </summary>
        Task<ApiResponse<Instrument>> AddInstrument(Instrument instrument);

        /// <summary>
        /// Add a new session template
        /// </summary>
        Task<ApiResponse<SessionTemplate>> AddSessionTemplate(SessionTemplate sessiontemplate);

        /// <summary>
        /// Add a new tag
        /// </summary>
        Task<ApiResponse<Tag>> AddTag(Tag tag);

        /// <summary>
        /// Cancel a live real time data stream.
        /// </summary>
        void CancelRealTimeData(Instrument instrument);

        /// <summary>
        /// Tries to connect to the QDMS server.
        /// </summary>
        void Connect();

        /// <summary>
        /// Delete an exchange
        /// </summary>
        Task<ApiResponse<Exchange>> DeleteExchange(Exchange exchange);

        /// <summary>
        /// Delete an instrument
        /// </summary>
        Task<ApiResponse<Instrument>> DeleteInstrument(Instrument instrument);

        /// <summary>
        /// Delete a session templaet
        /// </summary>
        Task<ApiResponse<SessionTemplate>> DeleteSessionTemplate(SessionTemplate sessiontemplate);

        /// <summary>
        /// Delete a tag
        /// </summary>
        Task<ApiResponse<Tag>> DeleteTag(Tag tag);

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        void Disconnect(bool cancelStreams);

        /// <summary>
        /// Get all datasources
        /// </summary>
        Task<ApiResponse<List<Datasource>>> GetDatasources();

        /// <summary>
        /// Get all datasources for economic releases
        /// </summary>
        Task<ApiResponse<List<string>>> GetEconomicReleaseDataSources();

        /// <summary>
        /// Get all economic releases
        /// </summary>
        Task<ApiResponse<List<EconomicRelease>>> GetEconomicReleases();

        /// <summary>
        /// Get economic releases
        /// </summary>
        Task<ApiResponse<List<EconomicRelease>>> GetEconomicReleases(EconomicReleaseRequest req);

        /// <summary>
        /// Get a specific exchange by id
        /// </summary>
        Task<ApiResponse<Exchange>> GetExchange(int id);

        /// <summary>
        /// Get all exchanges
        /// </summary>
        Task<ApiResponse<List<Exchange>>> GetExchanges();

        /// <summary>
        /// Get instrument by id
        /// </summary>
        Task<ApiResponse<Instrument>> GetInstrument(int id);

        /// <summary>
        /// Get all instruments
        /// </summary>
        Task<ApiResponse<List<Instrument>>> GetInstruments();

        /// <summary>
        /// Search for instruments using a predicate
        /// </summary>
        Task<ApiResponse<List<Instrument>>> GetInstruments(Expression<Func<Instrument, bool>> pred);

        /// <summary>
        /// Search for instruments using an instrument object
        /// Instruments with matching fields will be returned
        /// </summary>
        Task<ApiResponse<List<Instrument>>> GetInstruments(Instrument instrument);

        /// <summary>
        /// Requests information on what historical data is available in local storage for this instrument.
        /// </summary>
        /// <param name="instrument"></param>
        void GetLocallyAvailableDataInfo(Instrument instrument);

        /// <summary>
        /// Get all session templates
        /// </summary>
        Task<ApiResponse<List<SessionTemplate>>> GetSessionTemplates();

        /// <summary>
        /// Get all tags
        /// </summary>
        Task<ApiResponse<List<Tag>>> GetTags();

        /// <summary>
        /// Pushes data to local storage.
        /// </summary>
        void PushData(DataAdditionRequest request);
        /// <summary>
        /// Request historical data. Data will be delivered through the HistoricalDataReceived event.
        /// </summary>
        /// <returns>An ID uniquely identifying this historical data request.</returns>
        int RequestHistoricalData(HistoricalDataRequest request);

        /// <summary>
        /// Request a new real time data stream. Data will be delivered through the RealTimeDataReceived event.
        /// </summary>
        int RequestRealTimeData(RealTimeDataRequest request);
        /// <summary>
        /// Update an existing exchange with new values
        /// </summary>
        Task<ApiResponse<Exchange>> UpdateExchange(Exchange exchange);
        /// <summary>
        /// Update an existing instrument with new values
        /// </summary>
        Task<ApiResponse<Instrument>> UpdateInstrument(Instrument instrument);
        /// <summary>
        /// Update an existing session template with new values
        /// </summary>
        Task<ApiResponse<SessionTemplate>> UpdateSessionTemplate(SessionTemplate sessiontemplate);

        /// <summary>
        /// Update an existing tag with new values
        /// </summary>
        Task<ApiResponse<Tag>> UpdateTag(Tag tag);

        /// <summary>
        /// Get all data economic release update jobs
        /// </summary>
        Task<ApiResponse<List<EconomicReleaseUpdateJobSettings>>> GetEconomicReleaseUpdateJobs();

        /// <summary>
        /// Get all data update jobs
        /// </summary>
        Task<ApiResponse<List<DataUpdateJobSettings>>> GetaDataUpdateJobs();

        /// <summary>
        /// Get all dividend update jobs
        /// </summary>
        Task<ApiResponse<List<DividendUpdateJobSettings>>> GetDividendUpdateJobs();

        /// <summary>
        /// Add a new job
        /// </summary>
        Task <ApiResponse<T>> AddJob<T>(T job) where T : class, IJobSettings;

        /// <summary>
        /// Delete a job
        /// </summary>
        Task<ApiResponse<T>> DeleteJob<T>(T job) where T : class, IJobSettings;

        /// <summary>
        /// Get all underlying symbols
        /// </summary>
        Task<ApiResponse<List<UnderlyingSymbol>>> GetUnderlyingSymbols();

        /// <summary>
        /// Add a new underlying symbol
        /// </summary>
        Task<ApiResponse<UnderlyingSymbol>> AddUnderlyingSymbol(UnderlyingSymbol symbol);

        /// <summary>
        /// Update an existing underlying symbol
        /// </summary>
        Task<ApiResponse<UnderlyingSymbol>> UpdateUnderlyingSymbol(UnderlyingSymbol symbol);

        /// <summary>
        /// Delete an underlying symbol
        /// </summary>
        Task<ApiResponse<UnderlyingSymbol>> DeleteUnderlyingSymbol(UnderlyingSymbol symbol);

        /// <summary>
        /// Get dividends
        /// </summary>
        Task<ApiResponse<List<Dividend>>> GetDividends(DividendRequest req);

        /// <summary>
        /// Get all datasources for dividends
        /// </summary>
        Task<ApiResponse<List<string>>> GetDividendDataSources();
    }
}