// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseUpdateJob.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;
using NLog;
using QDMS.Server.Brokers;
using QDMS.Server.Jobs.JobDetails;
using QDMSServer;
using Quartz;
using System;
using System.Collections.Generic;

namespace QDMS.Server.Jobs
{
    public class EconomicReleaseUpdateJob : IJob
    {
        private readonly IEconomicReleaseBroker _broker;
        private readonly IEmailService _emailService;
        private readonly UpdateJobSettings _settings;
        private Logger _logger;
        private List<string> _errors = new List<string>();

        public EconomicReleaseUpdateJob(IEconomicReleaseBroker broker, IEmailService emailService, UpdateJobSettings settings)
        {
            _broker = broker;
            _emailService = emailService;
            _settings = settings;
        }

        /// <summary>
        /// Called by the <see cref="T:Quartz.IScheduler"/> when a <see cref="T:Quartz.ITrigger"/>
        ///             fires that is associated with the <see cref="T:Quartz.IJob"/>.
        /// </summary>
        /// <remarks>
        /// The implementation may wish to set a  result object on the
        ///             JobExecutionContext before this method exits.  The result itself
        ///             is meaningless to Quartz, but may be informative to
        ///             <see cref="T:Quartz.IJobListener"/>s or
        ///             <see cref="T:Quartz.ITriggerListener"/>s that are watching the job's
        ///             execution.
        /// </remarks>
        /// <param name="context">The execution context.</param>
        public void Execute(IJobExecutionContext context)
        {
            _logger = LogManager.GetCurrentClassLogger();

            JobDataMap dataMap = context.JobDetail.JobDataMap;
            EconomicReleaseUpdateJobSettings settings;
            try
            {
                settings = JsonConvert.DeserializeObject<EconomicReleaseUpdateJobSettings>((string)dataMap["settings"]);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to deserialize data update job settings");
                return;
            }

            _logger.Info($"Data Update job {settings.Name} triggered.");

            _broker.Error += _broker_Error;

            var startDate = DateTime.Now.AddBusinessDays(-settings.BusinessDaysBack);
            var endDate = DateTime.Now.AddBusinessDays(settings.BusinessDaysAhead);
            var req = new EconomicReleaseRequest(startDate, endDate, dataLocation: DataLocation.ExternalOnly, dataSource: settings.DataSource);
            var releases = _broker.RequestEconomicReleases(req).Result; //no async support in Quartz, and no need for it anyway, this runs on its own thread
            _logger.Trace($"Economic release update job downloaded {releases.Count} items");

            JobComplete();
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