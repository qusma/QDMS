// -----------------------------------------------------------------------
// <copyright file="DataUpdateJob.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using NLog;
using QDMS;
using Quartz;

namespace QDMSServer
{
    public class DataUpdateJob : IJob
    {
        private Logger _logger;
        private IEmailService _emailService;
        private IHistoricalDataBroker _broker;
        private List<string> _errors;
        private string _requesterID;
        private UpdateJobSettings _settings;
        private List<HistoricalDataRequest> _pendingRequests;
        private IDataStorage _localStorage;
        private object _reqIDLock = new object();
        private IInstrumentSource _instrumentManager;

        public DataUpdateJob(IHistoricalDataBroker broker, IEmailService emailService, UpdateJobSettings settings, IDataStorage localStorage, IInstrumentSource instrumentManager)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            if (localStorage == null) throw new ArgumentNullException("localStorage");
            if (instrumentManager == null) throw new ArgumentNullException("instrumentManager");

            _broker = broker;
            _emailService = emailService;
            _errors = new List<string>();
            _pendingRequests = new List<HistoricalDataRequest>();

            _settings = settings;
            _localStorage = localStorage;
            _instrumentManager = instrumentManager;
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

            if(_broker == null)
            {
                Log(LogLevel.Error, "Data Update Job failed: broker not set.");
                return;
            }

            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var details = (DataUpdateJobDetails)dataMap["details"];
            
            Log(LogLevel.Info, string.Format("Data Update job {0} triggered.", details.Name));

            //Multiple jobs may be called simultaneously, so what we do is seed the Random based on the job name
            byte[] bytes = new byte[details.Name.Length * sizeof(char)];
            Buffer.BlockCopy(details.Name.ToCharArray(), 0, bytes, 0, bytes.Length);
            Random r = new Random((int)DateTime.Now.TimeOfDay.TotalSeconds ^ BitConverter.ToInt32(bytes, 0));
            _requesterID = "DataUpdateJob" + r.Next(); //we use this ID to identify this particular data update job

            
            List<Instrument> instruments = details.UseTag 
                ? _instrumentManager.FindInstruments(pred: x => x.Tags.Any(y => y.ID == details.TagID)) 
                : _instrumentManager.FindInstruments(pred: x => x.ID == details.InstrumentID);

            if (instruments.Count == 0)
            {
                Log(LogLevel.Error, string.Format("Aborting data update job {0}: no instruments found.", details.Name));
                return;
            }

            _broker.HistoricalDataArrived += _broker_HistoricalDataArrived;
            _broker.Error += _broker_Error;

            int counter = 1;

            //What we do here: we check what we have available locally..
            //If there is something, we send a query to grab data between the last stored time and "now"
            //Otherwise we send a query to grab everything since 1900
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

                var storageInfo = _localStorage.GetStorageInfo(i.ID.Value);
                if (storageInfo.Any(x => x.Frequency == details.Frequency))
                {
                    var relevantStorageInfo = storageInfo.First(x => x.Frequency == details.Frequency);
                    startingDT = relevantStorageInfo.LatestDate;
                }

                var req = new HistoricalDataRequest(
                    i,
                    details.Frequency,
                    startingDT,
                    DateTime.Now, //TODO this should be in the instrument's timezone...
                    dataLocation: DataLocation.ExternalOnly,
                    saveToLocalStorage: true,
                    rthOnly: true,
                    requestID: counter)
                    {
                        RequesterIdentity = _requesterID
                    };

                try
                {
                    _broker.RequestHistoricalData(req);
                    lock (_reqIDLock)
                    {
                        _pendingRequests.Add(req);
                    }
                }
                catch(Exception ex)
                {
                    _errors.Add(ex.Message);
                }
                counter++;
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            //loop until time runs out or all requests are completed
            while (_pendingRequests.Count > 0 && 
                sw.ElapsedMilliseconds < _settings.Timeout * 1000)
            {
                Thread.Sleep(100);
            }

            JobComplete();

            Log(LogLevel.Info, string.Format("Data Update job {0} completed.", details.Name));
        }
        
        /// <summary>
        /// Any errors coming up from the broker are added to a list of errors to be emailed at the end of the job
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _broker_Error(object sender, ErrorArgs e)
        {
            if(_settings.Errors)
            {
                _errors.Add(e.ErrorMessage);
            }
        }

        void _broker_HistoricalDataArrived(object sender, HistoricalDataEventArgs e)
        {
            if (e.Request.RequesterIdentity != _requesterID) return;

            //If no data was received, we add that to the errors
            if(e.Data.Count == 0 && _settings.NoDataReceived)
            {
                _errors.Add(string.Format("Data update for instrument {0} downloaded 0 bars.", e.Request.Instrument));
            }

            //Check the data for abnormalities
            CheckDataForOutliers(e.Request);

            //Remove the request from pending ones
            lock (_reqIDLock)
            {
                var req = _pendingRequests.FirstOrDefault(x => x.RequestID == e.Request.RequestID);
                if (req != null)
                {
                    _pendingRequests.Remove(req);
                }
            }
        }

        private void JobComplete()
        {
            _broker.Error -= _broker_Error;
            _broker.HistoricalDataArrived -= _broker_HistoricalDataArrived;

            //If there are any unfulfilled requests, add them here
            if (_settings.RequestTimeouts)
            {
                foreach (var req in _pendingRequests)
                {
                    _errors.Add(string.Format("Data update request for instrument {0} could not be fulfilled.", req.Instrument));
                }
            }

            if(_errors.Count > 0 && _emailService != null && !string.IsNullOrEmpty(_settings.FromEmail) && !string.IsNullOrEmpty(_settings.ToEmail))
            {
                //No email specified, so there's nothing to do
                if(string.IsNullOrEmpty(_settings.ToEmail))
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
                catch(Exception ex)
                {
                    Log(LogLevel.Error, string.Format("Update job could not send error email: {0}", ex.Message));
                }
            }

            //Dispose stuff
            _localStorage.Dispose();
        }

        /// <summary>
        /// Simple check for any data abnormalities
        /// </summary>
        private void CheckDataForOutliers(HistoricalDataRequest request)
        {
            if (!_settings.Outliers) return;
            double abnormalLimit = 0.20;
            double abnormalStDevRange = 3;

            var inst = request.Instrument;
            var freq = request.Frequency;

            //Grab the data plus some slightly older for comparison
            List<OHLCBar> data = _localStorage.GetData(
                inst,
                request.StartingDate.Add(-TimeSpan.FromMinutes(freq.ToTimeSpan().Minutes * 5)),
                request.EndingDate,
                freq);

            if(data == null || data.Count <= 1) return;

            //count how many bars are not newly updated
            int toSkip = data.Count(x => x.DT < request.StartingDate);

            var closePrices = data
                .Select(x => x.AdjClose.HasValue ? (double)x.AdjClose.Value : (double)x.Close);
            var absRets = closePrices.Zip(closePrices.Skip(1), (x, y) => Math.Abs(y / x - 1));
            
            if(absRets.Skip(Math.Max(0, toSkip - 1)).Max() >= abnormalLimit)
            {
                _errors.Add(string.Format("Possible dirty data detected, abnormally large returns in instrument {0} at frequency {1}.",
                    inst,
                    freq));
            }

            //Check for abnormally large ranges
            var highs = data.Select(x => x.AdjHigh.HasValue ? x.AdjHigh.Value : x.High);
            var lows = data.Select(x => x.AdjLow.HasValue ? x.AdjLow.Value : x.Low);
            var ranges = highs.Zip(lows, (h, l) => (double) (h - l));

            double stDev = ranges.QDMSStandardDeviation();
            double mean = ranges.Average();

            if(ranges.Skip(toSkip).Any(x => x > mean + stDev * abnormalStDevRange))
            {
                _errors.Add(string.Format("Possible dirty data detected, abnormally large range in instrument {0} at frequency {1}.",
                    inst,
                    freq));
            }
        }

        private void Log(LogLevel level, string message)
        {
            _logger.Log(level, message);
        }
    }
}
