// -----------------------------------------------------------------------
// <copyright file="DataUpdateJob.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EntityData;
using NLog;
using QDMS;
using Quartz;

namespace QDMSServer
{
    public class DataUpdateJob : IJob
    {
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
            var logger = LogManager.GetCurrentClassLogger();

            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var details = (DataUpdateJobDetails)dataMap["details"];

            logger.Log(LogLevel.Info, string.Format("Data Update job {0} triggered.", details.Name));

            //Multiple jobs may be called simultaneously, so what we do is seed the Random based on the job name
            byte[] bytes = new byte[details.Name.Length * sizeof(char)];
            System.Buffer.BlockCopy(details.Name.ToCharArray(), 0, bytes, 0, bytes.Length);
            var r = new Random((int)DateTime.Now.TimeOfDay.TotalSeconds ^ BitConverter.ToInt32(bytes, 0));

            List<Instrument> instruments = new List<Instrument>();
            var im = new InstrumentManager();
            if (details.UseTag)
            {
                using (var dbContext = new MyDBContext())
                {
                    var tag = dbContext.Tags.FirstOrDefault(x => x.ID == details.TagID);
                    if (tag == null)
                    {
                        logger.Log(LogLevel.Info, string.Format("Aborting data update job {0}: tag not found.", details.Name));
                        return;
                    }
                    instruments = dbContext.Instruments.Where(x => x.Tags.Contains(tag)).ToList();
                }
            }
            else
            {
                instruments = im.FindInstruments(pred: x => x.ID == details.InstrumentID).ToList();
            }

            if (instruments.Count == 0)
            {
                logger.Log(LogLevel.Info, string.Format("Aborting data update job {0}: instrument not found.", details.Name));
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
                client.Connect();

                using (var localStorage = DataStorageFactory.Get())
                {
                    foreach (Instrument i in instruments)
                    {
                        if (!i.ID.HasValue) continue;

                        var storageInfo = localStorage.GetStorageInfo(i.ID.Value);
                        if (storageInfo.Any(x => x.Frequency == details.Frequency))
                        {
                            var relevantStorageInfo = storageInfo.First(x => x.Frequency == details.Frequency);
                            client.RequestHistoricalData(new HistoricalDataRequest(
                                i,
                                details.Frequency,
                                relevantStorageInfo.LatestDate,
                                DateTime.Now, //TODO this should be in the instrument's timezone...
                                forceFreshData: true,
                                localStorageOnly: false,
                                saveToLocalStorage: true,
                                rthOnly: true));
                        }
                    }
                }

                client.Disconnect();
            }

            logger.Log(LogLevel.Info, string.Format("Data Update job {0} completed.", details.Name));
        }
    }
}
