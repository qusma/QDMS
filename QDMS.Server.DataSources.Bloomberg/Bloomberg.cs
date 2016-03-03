// -----------------------------------------------------------------------
// <copyright file="Bloomberg.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// Created using the Bloomberg API emulator (https://bemu.codeplex.com/)
// Don't have access to the real deal, so not sure how well it works.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Bloomberglp.Blpapi;
using QDMS;
using QDMS.Annotations;

#pragma warning disable 67
namespace QDMSServer.DataSources
{
    public class Bloomberg : IHistoricalDataSource, IRealTimeDataSource
    {
        /// <summary>
        /// The name of the data source.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Whether the connection to the data source is up or not.
        /// </summary>
        public bool Connected { get; private set; }

        public Bloomberg(string host, int port)
        {
            Name = "Bloomberg";

            SessionOptions sessionOptions = new SessionOptions();
            sessionOptions.ServerHost = host;
            sessionOptions.ServerPort = port;

            Session session = new Session(sessionOptions);
            Connected = session.Start();

        }

        private void SendIntradayBarRequest(Session session, RealTimeDataRequest req)
        {
            session.OpenService("//blp/refdata");
            Service refDataService = session.GetService("//blp/refdata");
            Request request = refDataService.CreateRequest("IntradayBarRequest");

            // only one security/eventType per request
            request.Set("security", req.Instrument.DatasourceSymbol);
            request.Set("eventType", "TRADE");
            request.Set("interval", req.Frequency.ToTimeSpan().TotalSeconds);

            DateTime prevTradedDate = DateTime.Now.AddDays(-1); //todo fix
            var startDateTime = string.Format("{0}-{1}-{2}T13:30:00", prevTradedDate.Year, prevTradedDate.Month, prevTradedDate.Day);
            prevTradedDate = prevTradedDate.AddDays(1); // next day for end date
            var endDateTime = string.Format("{0}-{1}-{2}T13:30:00", prevTradedDate.Year, prevTradedDate.Month, prevTradedDate.Day);

            request.Set("startDateTime", startDateTime);
            request.Set("endDateTime", endDateTime);


            request.Set("gapFillInitialBar", true);

            session.SendRequest(request, null);
        }

        //TODO write
        private void EventLoop(Session session)
        {
            bool done = false;
            while (!done)
            {
                Event eventObj = session.NextEvent();
                if (eventObj.Type == Event.EventType.PARTIAL_RESPONSE)
                {
                    ProcessResponseEvent(eventObj, session);
                }
                else if (eventObj.Type == Event.EventType.RESPONSE)
                {
                    ProcessResponseEvent(eventObj, session);
                    done = true;
                }
                else
                {
                    foreach (Message msg in eventObj)
                    {
                        System.Console.WriteLine(msg.AsElement);
                        if (eventObj.Type == Event.EventType.SESSION_STATUS)
                        {
                            if (msg.MessageType.Equals("SessionTerminated"))
                            {
                                done = true;
                            }
                        }
                    }
                }
            }
        }


        //TODO write
        private void ProcessResponseEvent(Event eventObj, Session session)
        {
            foreach (Message msg in eventObj)
            {
                if (msg.HasElement(new Name("responseError")))
                {
                    //printErrorInfo("REQUEST FAILED: ", msg.GetElement(new Name("responseError")));
                    continue;
                }
                ProcessMessage(msg);
            }
        }

        //TODO write
        private void ProcessMessage(Message msg)
        {
            //Element data = msg.GetElement(BAR_DATA).GetElement(BAR_TICK_DATA);
            //int numBars = data.NumValues;
            //System.Console.WriteLine("Response contains " + numBars + " bars");
            //System.Console.WriteLine("Datetime\t\tOpen\t\tHigh\t\tLow\t\tClose" +
            //    "\t\tNumEvents\tVolume");
            //for (int i = 0; i < numBars; ++i)
            //{
            //    Element bar = data.GetValueAsElement(i);
            //    Datetime time = bar.GetElementAsDate(TIME);
            //    double open = bar.GetElementAsFloat64(OPEN);
            //    double high = bar.GetElementAsFloat64(HIGH);
            //    double low = bar.GetElementAsFloat64(LOW);
            //    double close = bar.GetElementAsFloat64(CLOSE);
            //    int numEvents = bar.GetElementAsInt32(NUM_EVENTS);
            //    long volume = bar.GetElementAsInt64(VOLUME);
            //    System.DateTime sysDatetime = time.ToSystemDateTime();
            //    System.Console.WriteLine(
            //        sysDatetime.ToString("s") + "\t" +
            //        open.ToString("C") + "\t\t" +
            //        high.ToString("C") + "\t\t" +
            //        low.ToString("C") + "\t\t" +
            //        close.ToString("C") + "\t\t" +
            //        numEvents + "\t\t" +
            //        volume);
            //}
        }

        /// <summary>
        /// Request real time data.
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The ID associated with this real time data request.</returns>
        public int RequestRealTimeData(RealTimeDataRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cancel a real time data stream.
        /// </summary>
        /// <param name="requestID">The ID of the real time data stream.</param>
        public void CancelRealTimeData(int requestID)
        {
            throw new NotImplementedException();
        }

        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Connect to the data source.
        /// </summary>
        public void Connect()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Disconnect from the data source.
        /// </summary>
        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<HistoricalDataEventArgs> HistoricalDataArrived;

        public event EventHandler<ErrorArgs> Error;

        public event EventHandler<RealTimeDataEventArgs> DataReceived;

        public event EventHandler<DataSourceDisconnectEventArgs> Disconnected;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
#pragma warning restore 67