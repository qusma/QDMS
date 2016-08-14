// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QDMS;

namespace SampleApp
{
    class Program
    {
        static void Main()
        {
            //create the client, assuming the default port settings
            QDMSClient.QDMSClient client = new QDMSClient.QDMSClient(
                "SampleClient",
                "127.0.0.1",
                5556,
                5557,
                5558,
                5555);

            //hook up the events needed to receive data & error messages
            client.HistoricalDataReceived += client_HistoricalDataReceived;
            client.RealTimeDataReceived += client_RealTimeDataReceived;
            client.LocallyAvailableDataInfoReceived += client_LocallyAvailableDataInfoReceived;
            client.Error += client_Error;

            //connect to the server
            client.Connect();
            
            //make sure the connection was succesful before we continue
            if (!client.Connected)
            {
                Console.WriteLine("Could not connect.");
                Console.WriteLine("Press enter to exit.");
                Console.ReadLine();
                return;
            }

            //request the list of available instruments
            List<Instrument> instruments = client.FindInstruments();
            foreach (Instrument i in instruments)
            {
                Console.WriteLine("Instrument ID {0}: {1} ({2}), Datasource: {3}",
                    i.ID,
                    i.Symbol,
                    i.Type,
                    i.Datasource.Name);
            }

            Thread.Sleep(3000);
            
            //then we grab some historical data from Yahoo
            //start by finding the SPY instrument
            var spy = client.FindInstruments(x => x.Symbol == "SPY" && x.Datasource.Name == "Yahoo").FirstOrDefault();
            if (spy != null)
            {
                var req = new HistoricalDataRequest(
                    spy,
                    BarSize.OneDay,
                    new DateTime(2013, 1, 1),
                    new DateTime(2013, 1, 15),
                    dataLocation: DataLocation.Both,
                    saveToLocalStorage: true,
                    rthOnly: true);

                client.RequestHistoricalData(req);


                Thread.Sleep(3000);

                //now that we downloaded the data, let's make a request to see what is stored locally
                client.GetLocallyAvailableDataInfo(spy);

                Thread.Sleep(3000);

                //finally send a real time data request (from the simulated data datasource)
                spy.Datasource.Name = "SIM";
                var rtReq = new RealTimeDataRequest(spy, BarSize.OneSecond);
                client.RequestRealTimeData(rtReq);

                Thread.Sleep(3000);

                //And then cancel the real time data stream
                client.CancelRealTimeData(spy);
            }

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
            client.Disconnect();
            client.Dispose();
        }

        static void client_LocallyAvailableDataInfoReceived(object sender, LocallyAvailableDataInfoReceivedEventArgs e)
        {
            foreach (StoredDataInfo s in e.StorageInfo)
            {
                Console.WriteLine("Freq: {0} - From {1} to {2}", s.Frequency, s.EarliestDate, s.LatestDate);
            }
        }

        static void client_Error(object sender, ErrorArgs e)
        {
            Console.WriteLine("Error {0}: {1}", e.ErrorCode, e.ErrorMessage);
        }

        static void client_RealTimeDataReceived(object sender, RealTimeDataEventArgs e)
        {
            Console.WriteLine("Real Time Data Received: O: {0}  H: {1}  L: {2}  C: {3}",
                e.Open,
                e.High,
                e.Low,
                e.Close);
        }

        static void client_HistoricalDataReceived(object sender, HistoricalDataEventArgs e)
        {
            Console.WriteLine("Historical data received:");
            foreach (OHLCBar bar in e.Data)
            {
                Console.WriteLine("{0} - O: {1}  H: {2}  L: {3}  C: {4}",
                    bar.DT,
                    bar.Open,
                    bar.High,
                    bar.Low,
                    bar.Close);
            }
        }
    }
}
