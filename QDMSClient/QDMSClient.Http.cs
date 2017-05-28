// -----------------------------------------------------------------------
// <copyright file="QDMSClient.Http.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace QDMSClient
{
    public partial class QDMSClient
    {
        private readonly ApiClient _apiClient;

        #region exchanges

        /// <summary>
        /// Get all exchanges
        /// </summary>
        public async Task<ApiResponse<List<Exchange>>> GetExchanges() =>
            await _apiClient.GetAsync<List<Exchange>>("/exchanges").ConfigureAwait(false);

        /// <summary>
        /// Get a specific exchange by id
        /// </summary>
        public async Task<ApiResponse<Exchange>> GetExchange(int id) =>
            await _apiClient.GetAsync<Exchange>($"/exchanges/{id}").ConfigureAwait(false);

        /// <summary>
        /// Add a new exchange
        /// </summary>
        public async Task<ApiResponse<Exchange>> AddExchange(Exchange exchange) =>
            await _apiClient.PostAsync<Exchange>("/exchanges", exchange).ConfigureAwait(false);

        /// <summary>
        /// Update an existing exchange with new values
        /// </summary>
        public async Task<ApiResponse<Exchange>> UpdateExchange(Exchange exchange) =>
            await _apiClient.PutAsync<Exchange>("/exchanges", exchange).ConfigureAwait(false);

        /// <summary>
        /// Delete an exchange
        /// </summary>
        public async Task<ApiResponse<Exchange>> DeleteExchange(Exchange exchange) =>
            await _apiClient.DeleteAsync<Exchange>($"/exchanges/{exchange?.ID}").ConfigureAwait(false);

        #endregion exchanges

        #region datasources

        /// <summary>
        /// Get all datasources
        /// </summary>
        public async Task<ApiResponse<List<Datasource>>> GetDatasources() =>
            await _apiClient.GetAsync<List<Datasource>>("/datasources").ConfigureAwait(false);

        /// <summary>
        /// Returns connection status for all datasources
        /// </summary>
        public async Task<ApiResponse<List<DataSourceStatus>>> GetDatasourceStatus() =>
            await _apiClient.GetAsync<List<DataSourceStatus>>("/datasources/status").ConfigureAwait(false);

        /// <summary>
        /// Returns active realtime data streams
        /// </summary>
        public async Task<ApiResponse<List<RealTimeStreamInfo>>> GetRealTimeStreamInfo() =>
            await _apiClient.GetAsync<List<RealTimeStreamInfo>>("/datasources/activestreams").ConfigureAwait(false);

        #endregion datasources

        #region economicReleases

        /// <summary>
        /// Get all economic releases
        /// </summary>
        public async Task<ApiResponse<List<EconomicRelease>>> GetEconomicReleases() =>
            await _apiClient.GetAsync<List<EconomicRelease>>("/economicreleases").ConfigureAwait(false);

        /// <summary>
        /// Get economic releases
        /// </summary>
        public async Task<ApiResponse<List<EconomicRelease>>> GetEconomicReleases(EconomicReleaseRequest req) =>
            await _apiClient.GetAsync<List<EconomicRelease>>("/economicreleases", req).ConfigureAwait(false);

        /// <summary>
        /// Get all datasources for economic releases
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetEconomicReleaseDataSources() =>
            await _apiClient.GetAsync<List<string>>("/economicreleases/datasources").ConfigureAwait(false);

        #endregion economicReleases

        #region instruments

        /// <summary>
        /// Get all instruments
        /// </summary>
        public async Task<ApiResponse<List<Instrument>>> GetInstruments() =>
            await _apiClient.GetAsync<List<Instrument>>("/instruments").ConfigureAwait(false);

        /// <summary>
        /// Search for instruments using a predicate
        /// </summary>
        public async Task<ApiResponse<List<Instrument>>> GetInstruments(Expression<Func<Instrument, bool>> pred) =>
            await _apiClient.GetAsync<List<Instrument>>("/instruments/predsearch", new PredicateSearchRequest(pred)).ConfigureAwait(false);

        /// <summary>
        /// Search for instruments using an instrument object
        /// Instruments with matching fields will be returned
        /// </summary>
        public async Task<ApiResponse<List<Instrument>>> GetInstruments(Instrument instrument) =>
            await _apiClient.GetAsync<List<Instrument>>("/instruments/search", instrument).ConfigureAwait(false);

        /// <summary>
        /// Get instrument by id
        /// </summary>
        public async Task<ApiResponse<Instrument>> GetInstrument(int id) =>
            await _apiClient.GetAsync<Instrument>($"/instruments/{id}").ConfigureAwait(false);

        /// <summary>
        /// Add a new instrument
        /// </summary>
        public async Task<ApiResponse<Instrument>> AddInstrument(Instrument instrument) =>
            await _apiClient.PostAsync<Instrument>("/instruments", instrument).ConfigureAwait(false);

        /// <summary>
        /// Update an existing instrument with new values
        /// </summary>
        public async Task<ApiResponse<Instrument>> UpdateInstrument(Instrument instrument) =>
            await _apiClient.PutAsync<Instrument>("/instruments", instrument).ConfigureAwait(false);

        /// <summary>
        /// Delete an instrument
        /// </summary>
        public async Task<ApiResponse<Instrument>> DeleteInstrument(Instrument instrument) =>
            await _apiClient.DeleteAsync<Instrument>($"/instruments/{instrument?.ID}").ConfigureAwait(false);

        #endregion instruments

        #region tags

        /// <summary>
        /// Get all tags
        /// </summary>
        public async Task<ApiResponse<List<Tag>>> GetTags() =>
            await _apiClient.GetAsync<List<Tag>>("/tags").ConfigureAwait(false);

        /// <summary>
        /// Add a new tag
        /// </summary>
        public async Task<ApiResponse<Tag>> AddTag(Tag tag) =>
            await _apiClient.PostAsync<Tag>("/tags", tag).ConfigureAwait(false);

        /// <summary>
        /// Update an existing tag with new values
        /// </summary>
        public async Task<ApiResponse<Tag>> UpdateTag(Tag tag) =>
            await _apiClient.PutAsync<Tag>("/tags", tag).ConfigureAwait(false);

        /// <summary>
        /// Delete a tag
        /// </summary>
        public async Task<ApiResponse<Tag>> DeleteTag(Tag tag) =>
            await _apiClient.DeleteAsync<Tag>($"/tags/{tag?.ID}").ConfigureAwait(false);

        #endregion tags

        #region sessiontemplates

        /// <summary>
        /// Get all session templates
        /// </summary>
        public async Task<ApiResponse<List<SessionTemplate>>> GetSessionTemplates() =>
            await _apiClient.GetAsync<List<SessionTemplate>>("/sessiontemplates").ConfigureAwait(false);

        /// <summary>
        /// Add a new session template
        /// </summary>
        public async Task<ApiResponse<SessionTemplate>> AddSessionTemplate(SessionTemplate sessiontemplate) =>
            await _apiClient.PostAsync<SessionTemplate>("/sessiontemplates", sessiontemplate).ConfigureAwait(false);

        /// <summary>
        /// Update an existing session template with new values
        /// </summary>
        public async Task<ApiResponse<SessionTemplate>> UpdateSessionTemplate(SessionTemplate sessiontemplate) =>
            await _apiClient.PutAsync<SessionTemplate>("/sessiontemplates", sessiontemplate).ConfigureAwait(false);

        /// <summary>
        /// Delete a session template
        /// </summary>
        public async Task<ApiResponse<SessionTemplate>> DeleteSessionTemplate(SessionTemplate sessiontemplate) =>
            await _apiClient.DeleteAsync<SessionTemplate>($"/sessiontemplates/{sessiontemplate?.ID}").ConfigureAwait(false);

        #endregion sessiontemplates

        #region jobs

        /// <summary>
        /// Get all data update jobs
        /// </summary>
        public async Task<ApiResponse<List<DataUpdateJobSettings>>> GetaDataUpdateJobs() =>
            await _apiClient.GetAsync<List<DataUpdateJobSettings>>("/jobs/dataupdatejobs").ConfigureAwait(false);

        /// <summary>
        /// Get all data economic release update jobs
        /// </summary>
        public async Task<ApiResponse<List<EconomicReleaseUpdateJobSettings>>> GetEconomicReleaseUpdateJobs() =>
            await _apiClient.GetAsync<List<EconomicReleaseUpdateJobSettings>>("/jobs/economicreleaseupdatejobs").ConfigureAwait(false);

        /// <summary>
        /// Get all dividend update jobs
        /// </summary>
        public async Task<ApiResponse<List<DividendUpdateJobSettings>>> GetDividendUpdateJobs() =>
            await _apiClient.GetAsync<List<DividendUpdateJobSettings>>("/jobs/dividendupdatejobs").ConfigureAwait(false);

        private string GetJobPathFromType(IJobSettings job)
        {
            if (job is DataUpdateJobSettings)
                return "/jobs/dataupdatejobs";
            if (job is EconomicReleaseUpdateJobSettings)
                return "/jobs/economicreleaseupdatejobs";
            if (job is DividendUpdateJobSettings)
                return "jobs/dividendupdatejobs";

            throw new NotImplementedException();
        }

        /// <summary>
        /// Add a new job
        /// </summary>
        public async Task<ApiResponse<T>> AddJob<T>(T job) where T : class, IJobSettings =>
            await _apiClient.PostAsync<T>(GetJobPathFromType(job), job).ConfigureAwait(false);

        /// <summary>
        /// Delete a job
        /// </summary>
        public async Task<ApiResponse<T>> DeleteJob<T>(T job) where T : class, IJobSettings =>
            await _apiClient.DeleteAsync<T>(GetJobPathFromType(job), job).ConfigureAwait(false);

        #endregion jobs

        #region underlyingsymbols

        /// <summary>
        /// Get all underlying symbols
        /// </summary>
        public async Task<ApiResponse<List<UnderlyingSymbol>>> GetUnderlyingSymbols() =>
            await _apiClient.GetAsync<List<UnderlyingSymbol>>("/underlyingsymbols").ConfigureAwait(false);

        /// <summary>
        /// Add a new underlying symbol
        /// </summary>
        public async Task<ApiResponse<UnderlyingSymbol>> AddUnderlyingSymbol(UnderlyingSymbol symbol) =>
            await _apiClient.PostAsync<UnderlyingSymbol>("/underlyingsymbols", symbol).ConfigureAwait(false);

        /// <summary>
        /// Update an existing underlying symbol
        /// </summary>
        public async Task<ApiResponse<UnderlyingSymbol>> UpdateUnderlyingSymbol(UnderlyingSymbol symbol) =>
            await _apiClient.PutAsync<UnderlyingSymbol>("/underlyingsymbols", symbol).ConfigureAwait(false);

        /// <summary>
        /// Delete an underlying symbol
        /// </summary>
        public async Task<ApiResponse<UnderlyingSymbol>> DeleteUnderlyingSymbol(UnderlyingSymbol symbol) =>
            await _apiClient.DeleteAsync<UnderlyingSymbol>($"/underlyingsymbols/{symbol?.ID}").ConfigureAwait(false);

        #endregion underlyingsymbols

        #region dividends

        /// <summary>
        /// Get dividends
        /// </summary>
        public async Task<ApiResponse<List<Dividend>>> GetDividends(DividendRequest req) =>
            await _apiClient.GetAsync<List<Dividend>>("/dividends", req).ConfigureAwait(false);

        /// <summary>
        /// Get all datasources for dividends
        /// </summary>
        public async Task<ApiResponse<List<string>>> GetDividendDataSources() =>
            await _apiClient.GetAsync<List<string>>("/dividends/datasources").ConfigureAwait(false);

        #endregion dividends
    }
}