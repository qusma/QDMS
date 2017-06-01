// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobFactory.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using EntityData;
using QDMS.Server;
using QDMS.Server.Brokers;
using QDMS.Server.Jobs;
using Quartz;
using Quartz.Spi;

namespace QDMSServer
{
    public class JobFactory : IJobFactory
    {
        private readonly IHistoricalDataBroker _hdb;
        private readonly IEconomicReleaseBroker _erb;
        private readonly IDividendsBroker _divb;

        private string _host;
        private int _port;
        private string _username;
        private string _password;
        private string _sender;
        private string _email;

        private UpdateJobSettings _updateJobSettings;
        private QDMS.IDataStorage _localStorage;

        public JobFactory(IHistoricalDataBroker hdb,
            string host,
            int port,
            string username,
            string password,
            string sender,
            string email,
            UpdateJobSettings updateJobSettings,
            QDMS.IDataStorage localStorage,
            IEconomicReleaseBroker erb,
            IDividendsBroker divb) : base()
        {
            _hdb = hdb;

            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _sender = sender;
            _email = email;
            _updateJobSettings = updateJobSettings;
            _localStorage = localStorage;
            _erb = erb;
            _divb = divb;
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

            throw new Exception("Uknown job type " + type);
        }

        private IJob GetEconomicReleaseJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return new EconomicReleaseUpdateJob(_erb, GetEmailSender(), _updateJobSettings);
        }

        private IJob GetDataUpdateJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            //only provide the email sender if data exists to properly initialize it with
            return new DataUpdateJob(_hdb, GetEmailSender(), _updateJobSettings, _localStorage, new InstrumentRepository(new MyDBContext()));
        }

        private IJob GetDividendUpdateJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return new DividendUpdateJob(_divb, GetEmailSender(), _updateJobSettings, new InstrumentRepository(new MyDBContext()));
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