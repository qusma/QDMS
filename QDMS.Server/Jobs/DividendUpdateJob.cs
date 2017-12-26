// -----------------------------------------------------------------------
// <copyright file="DividendUpdateJob.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using QDMS.Server.Brokers;
using QDMSServer;
using Quartz;

namespace QDMS.Server.Jobs
{
    public class DividendUpdateJob : IJob
    {
        private IDividendsBroker _broker;
        private IEmailService _emailService;
        private UpdateJobSettings _settings;
        private readonly IInstrumentSource _instrumentManager;
        private List<string> _errors = new List<string>();
        private Logger _logger;

        public DividendUpdateJob(IDividendsBroker broker, IEmailService emailService, UpdateJobSettings settings, IInstrumentSource instrumentManager)
        {
            _broker = broker;
            _emailService = emailService;
            _settings = settings;
            _instrumentManager = instrumentManager;
        }

        public void Execute(IJobExecutionContext context)
        {
            _logger = LogManager.GetCurrentClassLogger();

            JobDataMap dataMap = context.JobDetail.JobDataMap;
            DividendUpdateJobSettings settings;
            try
            {
                settings = JsonConvert.DeserializeObject<DividendUpdateJobSettings>((string)dataMap["settings"]);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to deserialize data update job settings");
                return;
            }

            _logger.Info($"Dividend Update job {settings.Name} triggered.");

            _broker.Error += _broker_Error;

            int totalCount = 0;
            var request = GenerateRequests(settings);
            try
            {
                var releases = _broker.RequestDividends(request).Result; //no async support in Quartz, and no need for it anyway, this runs on its own thread
                totalCount += releases.Count;
            }
            catch (Exception ex)
            {
                _errors.Add(ex.Message);
            }

            _logger.Trace($"Dividend update job downloaded {totalCount} items");

            JobComplete();
        }

        private DividendRequest GenerateRequests(DividendUpdateJobSettings settings)
        {
            var startDate = DateTime.Now.Date.AddBusinessDays(-settings.BusinessDaysBack);
            var endDate = DateTime.Now.Date.AddBusinessDays(settings.BusinessDaysAhead);

            if (!settings.UseTag)
            {
                //grab all symbols
                return new DividendRequest(
                    startDate,
                    endDate,
                    dataLocation: DataLocation.ExternalOnly,
                    dataSource: settings.DataSource);
            }
            else
            {
                //get the symbols using the tag filter
                var symbols = _instrumentManager
                    .FindInstruments(x => x.Tags.Any(y => y.ID == settings.TagID))
                    .Result
                    .Select(x => x.Symbol)
                    .Distinct()
                    .ToList();

                
                return new DividendRequest(
                    startDate,
                    endDate,
                    symbols.ToList(),
                    dataLocation: DataLocation.ExternalOnly,
                    dataSource: settings.DataSource);
            }
        }

        private void _broker_Error(object sender, ErrorArgs e)
        {
            _errors.Add(e.ErrorMessage);
        }

        private void JobComplete()
        {
            _broker.Error -= _broker_Error;

            if (_errors.Count > 0 && _emailService != null && !string.IsNullOrEmpty(_settings.FromEmail) && !string.IsNullOrEmpty(_settings.ToEmail))
            {
                //No email specified, so there's nothing to do
                if (string.IsNullOrEmpty(_settings.ToEmail))
                {
                    return;
                }

                //Try to send the mail with the errors
                try
                {
                    _emailService.Send(
                        _settings.FromEmail,
                        _settings.ToEmail,
                        "QDMS: Data Update Errors",
                        string.Join(Environment.NewLine, _errors));
                }
                catch (Exception ex)
                {
                    _logger.Error($"Update job could not send error email: {ex.Message}");
                }
            }
        }
    }
}
