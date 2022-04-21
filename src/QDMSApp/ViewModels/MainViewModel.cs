// -----------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using DynamicData;
using EntityData;
using MahApps.Metro.Controls.Dialogs;
using Nancy.Bootstrapper;
using Nancy.Hosting.Self;
using NLog;
using QDMS;
using QDMS.Server.Brokers;
using QDMS.Server.Repositories;
using QDMSApp.Properties;
using QDMSClient;
using Quartz;
using Quartz.Impl.Matchers;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;

namespace QDMSApp.ViewModels
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        public ObservableCollection<Instrument> Instruments { get; }
        public ConcurrentNotifierBlockingList<LogEventInfo> LogMessages { get; set; }

        public IRealTimeDataBroker RealTimeBroker { get; set; }

        public IHistoricalDataBroker HistoricalBroker { get; set; }
        public IEconomicReleaseBroker EconomicReleaseBroker { get; set; }
        public IDividendsBroker DividendBroker { get; set; }
        public IEarningsAnnouncementBroker EarningsAnnouncementBroker { get; set; }

        public ReactiveCommand<IList, Unit> DeleteInstrument { get; set; }
        public ReactiveCommand<Unit, Instrument> AddInstrumentManually { get; set; }
        public ReactiveCommand<Instrument, Instrument> CloneInstrument { get; set; }
        public ReactiveCommand<Instrument, Instrument> EditInstrument { get; set; }

        public string ClientStatus
        {
            get => _clientStatus;
            set => this.RaiseAndSetIfChanged(ref _clientStatus, value);
        }

        private readonly IDataClient _client;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IScheduler _scheduler;
        private readonly IRealTimeDataServer _realTimeServer;
        private readonly IHistoricalDataServer _historicalDataServer;
        private readonly INancyBootstrapper _nancyBootstrapper;
        private NancyHost _nancyHost;
        private string _clientStatus;

        public MainViewModel(
            IDataClient client,
            IScheduler scheduler,
            IRealTimeDataServer realTimeServer,
            IHistoricalDataServer historicalDataServer,
            IRealTimeDataBroker realTimeDataBroker,
            IHistoricalDataBroker historicalDataBroker,
            IEconomicReleaseBroker economicReleaseBroker,
            IDividendsBroker dividendBroker,
            IEarningsAnnouncementBroker earningsAnnouncementBroker,
            INancyBootstrapper nancyBootstrapper,
            IDialogCoordinator dialogCoordinator)
        {
            RealTimeBroker = realTimeDataBroker;
            HistoricalBroker = historicalDataBroker;
            EconomicReleaseBroker = economicReleaseBroker;
            DividendBroker = dividendBroker;
            EarningsAnnouncementBroker = earningsAnnouncementBroker;
            _client = client;
            _scheduler = scheduler;
            _realTimeServer = realTimeServer;
            _historicalDataServer = historicalDataServer;
            _nancyBootstrapper = nancyBootstrapper;
            _dialogCoordinator = dialogCoordinator;
            Instruments = new ObservableCollection<Instrument>();

            //Set up logging
            SetUpLogging();

            //Create commands
            CreateCommands();

            //Start brokers, servers, scheduler
            StartServers();

            //hook up client status
            this.WhenAnyValue(x => x._client.Connected)
                .Subscribe(x => ClientStatus = "Client Status: " + (x ? "Connected" : "Disconnected"));

            //Start the client
            client.Connect();

            //Load data
            LoadData();
        }

        private void SetUpLogging()
        {
            LogMessages = new ConcurrentNotifierBlockingList<LogEventInfo>();

            //target is where the log managers send their logs, here we grab the memory target which has a Subject to observe
            var target = LogManager.Configuration.AllTargets.Single(x => x.Name == "myTarget") as MemoryTarget;

            //we subscribe to the messages and send them all to the LogMessages collection
            target?.Messages.Subscribe(msg => LogMessages.TryAdd(msg));
        }

        private void CreateCommands()
        {
            //DeleteInstrument
            DeleteInstrument = ReactiveCommand.CreateFromTask<IList>(async selectedInstruments =>
            {
                if (selectedInstruments == null) return;
                List<Instrument> toDelete = selectedInstruments.OfType<Instrument>().ToList();
                if (toDelete.Count == 0) return;

                //Ask for confirmation
                if (selectedInstruments.Count == 1)
                {
                    var inst = (Instrument)selectedInstruments[0];
                    MessageDialogResult res = await _dialogCoordinator.ShowMessageAsync(
                        this,
                        "Delete",
                        $"Are you sure you want to delete {inst.Symbol} @ {inst.Datasource.Name}?",
                        MessageDialogStyle.AffirmativeAndNegative).ConfigureAwait(true);
                    if (res == MessageDialogResult.Negative) return;
                }
                else
                {
                    MessageDialogResult res = await _dialogCoordinator.ShowMessageAsync(
                        this,
                        "Delete",
                        $"Are you sure you want to delete {selectedInstruments.Count} instruments?",
                        MessageDialogStyle.AffirmativeAndNegative).ConfigureAwait(true);
                    if (res == MessageDialogResult.Negative) return;
                }

                foreach (Instrument inst in toDelete)
                {
                    ApiResponse<Instrument> result = await _client.DeleteInstrument(inst).ConfigureAwait(true);
                    bool hadErrors = await result.DisplayErrors(this, _dialogCoordinator).ConfigureAwait(true);
                    if (!hadErrors)
                    {
                        Instruments.Remove(inst);
                    }
                }
            });

            //AddInstrumentManually
            AddInstrumentManually = ReactiveCommand.Create(() =>
            {
                var window = new AddInstrumentManuallyWindow(_client);
                window.ShowDialog();
                return window.ViewModel.AddedInstrument;
            });
            AddInstrumentManually.Where(x => x != null).Subscribe(x => Instruments.Add(x));

            //CloneInstrument
            CloneInstrument = ReactiveCommand.Create<Instrument, Instrument>(inst =>
            {
                var window = new AddInstrumentManuallyWindow(_client, inst, true);
                window.ShowDialog();
                return window.ViewModel.AddedInstrument;
            });
            CloneInstrument.Where(x => x != null).Subscribe(newInst => Instruments.Add(newInst));

            //EditInstrument
            EditInstrument = ReactiveCommand.Create<Instrument, Instrument>(inst =>
            {
                var window = new AddInstrumentManuallyWindow(_client, inst, false);
                window.ShowDialog();
                return window.ViewModel.AddedInstrument;
            });
            EditInstrument.Where(x => x != null).Subscribe(newInst =>
            {
                int index = Instruments.IndexOf(x => x.ID == newInst.ID);
                Instruments[index] = newInst;
            });
        }

        private void LoadData()
        {
            var instrumentsRes = _client.GetInstruments().Result;

            if (instrumentsRes.WasSuccessful)
            {
                Instruments.AddRange(instrumentsRes.Result);
            }
            else
            {
                _logger.Error("Failed to load instruments: " + string.Join(", ", instrumentsRes.Errors));
            }
        }

        private void StartServers()
        {
            //Start servers
            _realTimeServer.StartServer();
            _historicalDataServer.StartServer();

            //start scheduler
            _scheduler.StartDelayed(TimeSpan.FromSeconds(10));

            //Start http server
            try
            {
                StartHttpServer();
            }
            catch (HttpListenerException ex)
            {
                //wrong SSL settings can cause a crash right at the start, which can be a pain
                _logger.Error(ex, "Nancy failed to start, try alternate SSL settings");
                throw;
            }
        }

        private void StartHttpServer()
        {
            var uri = new Uri(
                (Settings.Default.useSsl ? "https" : "http") + "://localhost:" + Settings.Default.httpPort);

            _nancyHost = new NancyHost(_nancyBootstrapper, uri);
            _nancyHost.Start();
        }

        public void Dispose()
        {
            _scheduler.Shutdown(true);

            _realTimeServer.StopServer();
            _realTimeServer.Dispose();

            _historicalDataServer.StopServer();
            _historicalDataServer.Dispose();

            _nancyHost.Dispose();

            RealTimeBroker.Dispose();
            //HDB is disposed automatically by the Nancy IoC container
        }
    }
}