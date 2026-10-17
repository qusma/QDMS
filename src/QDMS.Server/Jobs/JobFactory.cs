// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobFactory.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.EntityFrameworkCore;
using QDMS.Server.Brokers;
using Quartz;
using Quartz.Extensibility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QDMS.Server.Jobs
{
    /// <summary>
    /// Builds update jobs. The job type is taken from the job key's group (see <see cref="JobTypes"/>).
    /// Jobs that need the database get their own DbContext, disposed when the job is returned.
    /// </summary>
    public class JobFactory : IJobFactory
    {
        private readonly IHistoricalDataBroker _hdb;
        private readonly IEconomicReleaseBroker _erb;
        private readonly IDividendsBroker _divb;
        private readonly IEarningsAnnouncementBroker _eab;
        private readonly IDbContextFactory<MyDBContext> _contextFactory;
        private readonly IDataStorage _localStorage;
        private readonly ISettings _settings;
        private readonly UpdateJobSettings _updateJobSettings;

        public JobFactory(IHistoricalDataBroker hdb,
            ISettings settings,
            IDataStorage localStorage,
            IEconomicReleaseBroker erb,
            IDividendsBroker divb,
            IEarningsAnnouncementBroker eab,
            IDbContextFactory<MyDBContext> contextFactory)
        {
            _hdb = hdb;
            _settings = settings;
            _localStorage = localStorage;
            _erb = erb;
            _divb = divb;
            _eab = eab;
            _contextFactory = contextFactory;
            _updateJobSettings = new UpdateJobSettings(
                noDataReceived: settings.updateJobReportNoData,
                errors: settings.updateJobReportErrors,
                outliers: settings.updateJobReportOutliers,
                requestTimeouts: settings.updateJobTimeouts,
                timeout: settings.updateJobTimeout,
                toEmail: settings.updateJobEmail,
                fromEmail: settings.updateJobEmailSender);
        }

        public ValueTask<JobScope> CreateJob(TriggerFiredBundle bundle, IScheduler scheduler, CancellationToken cancellationToken)
        {
            //the job key's group, not the trigger's: a job run on demand fires with a trigger in the DEFAULT group
            string type = bundle.JobDetail.Key.Group;

            if (type == JobTypes.EconomicRelease)
            {
                return new ValueTask<JobScope>(new JobScope(new EconomicReleaseUpdateJob(_erb, GetEmailSender(), _updateJobSettings), null));
            }

            MyDBContext context = _contextFactory.CreateDbContext();
            var instrumentRepo = new InstrumentRepository(context);
            IJob job;
            if (type == JobTypes.DataUpdate)
            {
                job = new DataUpdateJob(_hdb, GetEmailSender(), _updateJobSettings, _localStorage, instrumentRepo);
            }
            else if (type == JobTypes.DividendUpdate)
            {
                job = new DividendUpdateJob(_divb, GetEmailSender(), _updateJobSettings, instrumentRepo);
            }
            else if (type == JobTypes.EarningsUpdate)
            {
                job = new EarningsUpdateJob(_eab, GetEmailSender(), _updateJobSettings, instrumentRepo);
            }
            else
            {
                context.Dispose();
                throw new Exception("Unknown job type " + type);
            }

            return new ValueTask<JobScope>(new JobScope(job, context));
        }

        public ValueTask ReturnJob(JobScope scope, CancellationToken cancellationToken)
        {
            (scope.State as IDisposable)?.Dispose();
            return default;
        }

        //only provide the email sender if there are settings to initialize it with
        private EmailSender GetEmailSender()
        {
            if (string.IsNullOrEmpty(_settings.updateJobEmailHost) ||
                string.IsNullOrEmpty(_settings.updateJobEmailUsername) ||
                string.IsNullOrEmpty(_settings.updateJobEmailPassword) ||
                string.IsNullOrEmpty(_settings.updateJobEmailSender) ||
                string.IsNullOrEmpty(_settings.updateJobEmail))
            {
                return null;
            }

            return new EmailSender(
                _settings.updateJobEmailHost,
                _settings.updateJobEmailUsername,
                _settings.updateJobEmailPassword,
                _settings.updateJobEmailPort);
        }
    }
}
