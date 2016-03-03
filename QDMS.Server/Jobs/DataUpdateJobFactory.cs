// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobFactory.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using Quartz;
using Quartz.Spi;

namespace QDMSServer
{
    public class DataUpdateJobFactory : IJobFactory
    {
        private readonly HistoricalDataBroker _hdb;

        private string _host;
        private int _port;
        private string _username;
        private string _password;
        private string _sender;
        private string _email;

        private UpdateJobSettings _updateJobSettings;
        private QDMS.IDataStorage _localStorage;
        private IInstrumentSource _instrumentSource;

        public DataUpdateJobFactory(HistoricalDataBroker broker,
            string host,
            int port,
            string username,
            string password,
            string sender,
            string email,
            UpdateJobSettings updateJobSettings,
            QDMS.IDataStorage localStorage,
            IInstrumentSource instrumentSource) : base()
        {
            _hdb = broker;

            _host = host;
            _port = port;
            _username = username;
            _password = password;
            _sender = sender;
            _email = email;
            _updateJobSettings = updateJobSettings;
            _localStorage = localStorage;
            _instrumentSource = instrumentSource;
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
            //only provide the email sender if data exists to properly initialize it with
            if (!string.IsNullOrEmpty(_host) &&
                !string.IsNullOrEmpty(_username) &&
                !string.IsNullOrEmpty(_password) &&
                !string.IsNullOrEmpty(_sender) &&
                !string.IsNullOrEmpty(_email))
            {
                return new DataUpdateJob(_hdb, new EmailSender(
                    _host,
                    _username,
                    _password,
                    _port), _updateJobSettings, _localStorage, _instrumentSource);
            }
            else
            {
                return new DataUpdateJob(_hdb, null, _updateJobSettings, _localStorage, _instrumentSource);
            }
        }

        /// <summary>
        /// Allows the the job factory to destroy/cleanup the job if needed.
        /// </summary>
        public void ReturnJob(IJob job)
        {
            
        }
    }
}