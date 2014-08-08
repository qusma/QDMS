// -----------------------------------------------------------------------
// <copyright file="DataUpdateJob.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using NLog;
using QDMS;
using Quartz;

namespace QDMSServer
{
    [DisallowConcurrentExecutionAttribute]
    public class DataUpdateJob : IJob
    {
        private Logger _logger;
        private string _jobName;
        private IEmailService _emailService;

        public DataUpdateJob(IEmailService emailService = null)
        {
            if(emailService == null)
            {
                //TODO write
            }

            _emailService = emailService;
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
            var details = (DataUpdateJobDetails)dataMap["details"];

            _jobName = details.Name;

            Log(LogLevel.Info, string.Format("Data Update job {0} triggered.", details.Name));

            //Multiple jobs may be called simultaneously, so what we do is seed the Random based on the job name
            byte[] bytes = new byte[details.Name.Length * sizeof(char)];
            Buffer.BlockCopy(details.Name.ToCharArray(), 0, bytes, 0, bytes.Length);
            var r = new Random((int)DateTime.Now.TimeOfDay.TotalSeconds ^ BitConverter.ToInt32(bytes, 0));

            var im = new InstrumentManager();
            List<Instrument> instruments = details.UseTag 
                ? im.FindInstruments(pred: x => x.Tags.Any(y => y.ID == details.TagID)) 
                : im.FindInstruments(pred: x => x.ID == details.InstrumentID);

            if (instruments.Count == 0)
            {
                Log(LogLevel.Error, string.Format("Aborting data update job {0}: no instruments found.", details.Name));
                return;
            }

            using (var client = new QDMSClient.QDMSClient(
                "DataUpdateJobClient" + r.Next().ToString(),
                "127.0.0.1",
                Properties.Settings.Default.rtDBReqPort,
                Properties.Settings.Default.rtDBPubPort,
                Properties.Settings.Default.instrumentServerPort,
                Properties.Settings.Default.hDBPort))
            {
                //try to connect
                try
                {
                    client.Connect();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, string.Format("Aborting data update job {0}: connection error {1}", details.Name, ex.Message));
                    return;
                }

                //Hook up the error event, we want to log that stuff
                client.Error += client_Error;

                //What we do here: we check what we have available locally..
                //If there is something, we send a query to grab data between the last stored time and "now"
                //Otherwise we send a query to grab everything since 1900
                using (var localStorage = DataStorageFactory.Get())
                {
                    foreach (Instrument i in instruments)
                    {
                        if (!i.ID.HasValue) continue;

                        //don't request data on expired securities unless the expiration was recent
                        if (i.Expiration.HasValue && (DateTime.Now - i.Expiration.Value).TotalDays > 15)
                        {
                            Log(LogLevel.Trace, string.Format("Data update job {0}: ignored instrument w/ ID {1} due to expiration date.", details.Name, i.ID));
                            continue;
                        }

                        DateTime startingDT = new DateTime(1900, 1, 1);

                        var storageInfo = localStorage.GetStorageInfo(i.ID.Value);
                        if (storageInfo.Any(x => x.Frequency == details.Frequency))
                        {
                            var relevantStorageInfo = storageInfo.First(x => x.Frequency == details.Frequency);
                            startingDT = relevantStorageInfo.LatestDate;
                        }

                        client.RequestHistoricalData(new HistoricalDataRequest(
                            i,
                            details.Frequency,
                            startingDT,
                            DateTime.Now, //TODO this should be in the instrument's timezone...
                            dataLocation: DataLocation.ExternalOnly,
                            saveToLocalStorage: true,
                            rthOnly: true));
                    }
                }

                //Requests aren't sent immediately so wait before killing the client to make sure the request gets to the server
                Thread.Sleep(50);

                client.Disconnect();
            }

            Log(LogLevel.Info, string.Format("Data Update job {0} completed.", details.Name));
        }

        void client_Error(object sender, ErrorArgs e)
        {
            Log(LogLevel.Error, string.Format("QDMSClient error in data update job {0}: {1}", _jobName, e.ErrorMessage));
        }

        private void Log(LogLevel level, string message)
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke(()
                    => _logger.Log(level, message));
        }
    }
}
