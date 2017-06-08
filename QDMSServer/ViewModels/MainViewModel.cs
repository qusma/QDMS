// -----------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using EntityData;
using MahApps.Metro.Controls.Dialogs;
using Nancy.Hosting.Self;
using NLog;
using QDMS;
using QDMS.Server.Brokers;
using QDMS.Server.DataSources;
using QDMS.Server.DataSources.Nasdaq;
using QDMS.Server.Nancy;
using QDMS.Server.Repositories;
using QDMSServer.DataSources;
using QDMSServer.Properties;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using ReactiveUI;

namespace QDMSServer.ViewModels
{
    public class MainViewModel : ReactiveObject, IDisposable
    {

        public ReactiveList<Instrument> Instruments { get; }
        public ConcurrentNotifierBlockingList<LogEventInfo> LogMessages { get; set; }

        public RealTimeDataBroker RealTimeBroker { get; set; }

        public HistoricalDataBroker HistoricalBroker { get; set; }
        public EconomicReleaseBroker EconomicReleaseBroker { get; set; }

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
        private IScheduler _scheduler;
        private RealTimeDataServer _realTimeServer;
        private HistoricalDataServer _historicalDataServer;
        private string _clientStatus;

        public MainViewModel(IDataClient client, IDialogCoordinator dialogCoordinator)
        {
            _client = client;
            _dialogCoordinator = dialogCoordinator;
            Instruments = new ReactiveList<Instrument>();

            //Set up logging
            LogMessages = new ConcurrentNotifierBlockingList<LogEventInfo>();

            //target is where the log managers send their logs, here we grab the memory target which has a Subject to observe
            var target = LogManager.Configuration.AllTargets.Single(x => x.Name == "myTarget") as MemoryTarget;

            //we subscribe to the messages and send them all to the LogMessages collection
            target?.Messages.Subscribe(msg => LogMessages.TryAdd(msg));

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

        private void CreateCommands()
        {
            //DeleteInstrument
            DeleteInstrument = ReactiveCommand.CreateFromTask<IList>(async selectedInstruments =>
            {
                if (selectedInstruments == null) return;
                var toDelete = selectedInstruments.OfType<Instrument>().ToList();
                if (toDelete.Count == 0) return;

                //Ask for confirmation
                if (selectedInstruments.Count == 1)
                {
                    var inst = (Instrument)selectedInstruments[0];
                    var res = await _dialogCoordinator.ShowMessageAsync(
                        this,
                        "Delete",
                        $"Are you sure you want to delete {inst.Symbol} @ {inst.Datasource.Name}?", 
                        MessageDialogStyle.AffirmativeAndNegative).ConfigureAwait(true);
                    if (res == MessageDialogResult.Negative) return;
                }
                else
                {
                    var res = await _dialogCoordinator.ShowMessageAsync(
                        this,
                        "Delete",
                        $"Are you sure you want to delete {selectedInstruments.Count} instruments?",
                        MessageDialogStyle.AffirmativeAndNegative).ConfigureAwait(true);
                    if (res == MessageDialogResult.Negative) return;
                }

                foreach (var inst in toDelete)
                {
                    var result = await _client.DeleteInstrument(inst).ConfigureAwait(true);
                    var hadErrors = await result.DisplayErrors(this, _dialogCoordinator).ConfigureAwait(true);
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
            var entityContext = new MyDBContext();

            //create brokers
            var cfRealtimeBroker = new ContinuousFuturesBroker(new QDMSClient.QDMSClient(
                    "RTDBCFClient",
                    "127.0.0.1",
                    Properties.Settings.Default.rtDBReqPort,
                    Properties.Settings.Default.rtDBPubPort,
                    Properties.Settings.Default.hDBPort,
                    Properties.Settings.Default.httpPort,
                    Properties.Settings.Default.apiKey,
                    useSsl: Properties.Settings.Default.useSsl),
                connectImmediately: false);
            var cfHistoricalBroker = new ContinuousFuturesBroker(new QDMSClient.QDMSClient(
                    "HDBCFClient",
                    "127.0.0.1",
                    Properties.Settings.Default.rtDBReqPort,
                    Properties.Settings.Default.rtDBPubPort,
                    Properties.Settings.Default.hDBPort,
                    Properties.Settings.Default.httpPort,
                    Properties.Settings.Default.apiKey,
                    useSsl: Properties.Settings.Default.useSsl),
                connectImmediately: false);
            var localStorage = DataStorageFactory.Get();
            RealTimeBroker = new RealTimeDataBroker(cfRealtimeBroker, localStorage,
                new IRealTimeDataSource[] {
                    //new Xignite(Properties.Settings.Default.xigniteApiToken),
                    //new Oanda(Properties.Settings.Default.oandaAccountId, Properties.Settings.Default.oandaAccessToken),
                    new IB(Properties.Settings.Default.ibClientHost, Properties.Settings.Default.ibClientPort, Properties.Settings.Default.rtdClientIBID),
                    //new ForexFeed(Properties.Settings.Default.forexFeedAccessKey, ForexFeed.PriceType.Mid)
                });
            HistoricalBroker = new HistoricalDataBroker(cfHistoricalBroker, localStorage,
                new IHistoricalDataSource[] {
                    new Yahoo(),
                    new FRED(),
                    //new Forexite(),
                    new IB(Properties.Settings.Default.ibClientHost, Properties.Settings.Default.ibClientPort, Properties.Settings.Default.histClientIBID),
                    new Quandl(Properties.Settings.Default.quandlAuthCode),
                    new BarChart(Properties.Settings.Default.barChartApiKey)
                });

            var countryCodeHelper = new CountryCodeHelper(entityContext.Countries.ToList());

            EconomicReleaseBroker = new EconomicReleaseBroker("FXStreet",
                new[] { new fx.FXStreet(countryCodeHelper) });

            DividendBroker = new DividendsBroker("Nasdaq",
                new[] { new NasdaqDs.Nasdaq() });

            //create the various servers
            _realTimeServer = new RealTimeDataServer(Properties.Settings.Default.rtDBPubPort, Properties.Settings.Default.rtDBReqPort, RealTimeBroker);
            _historicalDataServer = new HistoricalDataServer(Properties.Settings.Default.hDBPort, HistoricalBroker);

            //and start them
            _realTimeServer.StartServer();
            _historicalDataServer.StartServer();

            //create the scheduler
            var quartzSettings = QuartzUtils.GetQuartzSettings(Settings.Default.databaseType);
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory(quartzSettings);
            _scheduler = schedulerFactory.GetScheduler();
            _scheduler.JobFactory = new JobFactory(HistoricalBroker,
                Properties.Settings.Default.updateJobEmailHost,
                Properties.Settings.Default.updateJobEmailPort,
                Properties.Settings.Default.updateJobEmailUsername,
                Properties.Settings.Default.updateJobEmailPassword,
                Properties.Settings.Default.updateJobEmailSender,
                Properties.Settings.Default.updateJobEmail,
                new UpdateJobSettings(
                    noDataReceived: Properties.Settings.Default.updateJobReportNoData,
                    errors: Properties.Settings.Default.updateJobReportErrors,
                    outliers: Properties.Settings.Default.updateJobReportOutliers,
                    requestTimeouts: Properties.Settings.Default.updateJobTimeouts,
                    timeout: Properties.Settings.Default.updateJobTimeout,
                    toEmail: Properties.Settings.Default.updateJobEmail,
                    fromEmail: Properties.Settings.Default.updateJobEmailSender),
                localStorage,
                EconomicReleaseBroker,
                DividendBroker);
            _scheduler.Start();

            //Create http server
            var bootstrapper = new CustomBootstrapper(
                DataStorageFactory.Get(),
                EconomicReleaseBroker,
                HistoricalBroker,
                RealTimeBroker,
                DividendBroker,
                _scheduler,
                Properties.Settings.Default.apiKey);
            var uri = new Uri((Settings.Default.useSsl ? "https" : "http") + "://localhost:" + Properties.Settings.Default.httpPort);
            var host = new NancyHost(bootstrapper, uri);
            host.Start();

            //Take jobs stored in the qmds db and move them to the quartz db - this can be removed in the next version
            MigrateJobs(entityContext, _scheduler);

            entityContext.Dispose();
        }

        private void MigrateJobs(MyDBContext context, IScheduler scheduler)
        {
            //Check if there are jobs in the QDMS db and no jobs in the quartz db - in that case we migrate them
            var repo = new JobsRepository(context, scheduler);
            if (context.DataUpdateJobs.Any() && scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup()).Count == 0)
            {
                var jobs = context.DataUpdateJobs.Include("Tag").ToList();
                foreach (DataUpdateJobSettings job in jobs)
                {
                    repo.ScheduleJob(job);
                }
            }
        }

        public DividendsBroker DividendBroker { get; set; }
        public void Dispose()
        {
            _scheduler.Shutdown(true);

            _realTimeServer.StopServer();
            _realTimeServer.Dispose();

            _historicalDataServer.StopServer();
            _historicalDataServer.Dispose();

            RealTimeBroker.Dispose();

            HistoricalBroker.Dispose();
        }
    }
}
