// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobFactory.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using EntityData;
using QDMS;
using QDMS.Server;
using QDMS.Server.Brokers;
using QDMS.Server.Jobs;
using Quartz;
using Quartz.Spi;

namespace QDMSApp
{
    public class JobFactory : IJobFactory
    {
        private readonly IHistoricalDataBroker _hdb;
        private readonly IEconomicReleaseBroker _erb;
        private readonly IDividendsBroker _divb;
        private readonly IEarningsAnnouncementBroker _eab;

        private string _host;
        private int _port;
        private string _username;
        private string _password;
        private string _sender;
        private string _email;

        private UpdateJobSettings _updateJobSettings;
        private QDMS.IDataStorage _localStorage;

        public JobFactory(IHistoricalDataBroker hdb,
            ISettings settings,
            QDMS.IDataStorage localStorage,
            IEconomicReleaseBroker erb,
            IDividendsBroker divb,
            IEarningsAnnouncementBroker eab) : base()
        {
            _hdb = hdb;

            _host = settings.updateJobEmailHost;
            _port = settings.updateJobEmailPort;
            _username = settings.updateJobEmailUsername;
            _password = settings.updateJobEmailPassword;
            _sender = settings.updateJobEmailSender;
            _email = settings.updateJobEmail;
            _updateJobSettings = new UpdateJobSettings(
                noDataReceived: settings.updateJobReportNoData,
                errors: settings.updateJobReportErrors,
                outliers: settings.updateJobReportOutliers,
                requestTimeouts: settings.updateJobTimeouts,
                timeout: settings.updateJobTimeout,
                toEmail: settings.updateJobEmail,
                fromEmail: settings.updateJobEmailSender);
            _localStorage = localStorage;
            _erb = erb;
            _divb = divb;
            _eab = eab;
        }

        /// <summary>
        /// Called by the scheduler at the time of the trigger firing, in order to
        ///             produce a <see cref="T:Quartz.IJob"/> instance on which to call Execute.
        /// </summary>
        /// <remarks>
        /// It should be extremely rare for this method to throw an exception -
        ///             basically only the the case where there is no way at all to instantiate
        ///             and prepare the Job for execution.  When the exception is thrown, the
        ///             Scheduler will move all triggers associated with the Job into the
        ///             <see cref="F:Quartz.TriggerState.Error"/> state, which will require human
        ///             intervention (e.g. an application restart after fixing whatever 
        ///             configuration problem led to the issue wih instantiating the Job. 
        /// </remarks>
        /// <param name="bundle">The TriggerFiredBundle from which the <see cref="T:Quartz.IJobDetail"/>
        ///               and other info relating to the trigger firing can be obtained.
        ///             </param><param name="scheduler">a handle to the scheduler that is about to execute the job</param><throws>SchedulerException if there is a problem instantiating the Job. </throws>
        /// <returns>
        /// the newly instantiated Job
        /// </returns>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            string type = bundle.Trigger.Key.Group;

            if (type == JobTypes.DataUpdate)
            {
                return GetDataUpdateJob(bundle, scheduler);
            }
            else if (type == JobTypes.EconomicRelease)
            {
                return GetEconomicReleaseJob(bundle, scheduler);
            }
            else if (type == JobTypes.DividendUpdate)
            {
                return GetDividendUpdateJob(bundle, scheduler);
            }
            else if (type == JobTypes.EarningsUpdate)
            {
                return GetEarningsUpdateJob(bundle, scheduler);
            }

            throw new Exception("Uknown job type " + type);
        }

        private IJob GetEconomicReleaseJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return new EconomicReleaseUpdateJob(_erb, GetEmailSender(), _updateJobSettings);
        }

        private IJob GetDataUpdateJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            //only provide the email sender if data exists to properly initialize it with
            return new QDMSServer.DataUpdateJob(_hdb, GetEmailSender(), _updateJobSettings, _localStorage, new InstrumentRepository(new MyDBContext()));
        }

        private IJob GetDividendUpdateJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return new DividendUpdateJob(_divb, GetEmailSender(), _updateJobSettings, new InstrumentRepository(new MyDBContext()));
        }

        private IJob GetEarningsUpdateJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return new EarningsUpdateJob(_eab, GetEmailSender(), _updateJobSettings, new InstrumentRepository(new MyDBContext()));
        }

        private EmailSender GetEmailSender()
        {
            if (!string.IsNullOrEmpty(_host) &&
            !string.IsNullOrEmpty(_username) &&
            !string.IsNullOrEmpty(_password) &&
            !string.IsNullOrEmpty(_sender) &&
            !string.IsNullOrEmpty(_email))
            {
                return new EmailSender(
                            _host,
                            _username,
                            _password,
                            _port);
            }

            return null;
        }

        /// <summary>
        /// Allows the the job factory to destroy/cleanup the job if needed.
        /// </summary>
        public void ReturnJob(IJob job)
        {
            
        }
    }
}