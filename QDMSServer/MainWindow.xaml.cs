// -----------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data.Entity;
using System.Deployment.Application;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Common.Logging.NLog;
using EntityData;
using MahApps.Metro.Controls;
using NLog;
using NLog.Targets;
using QDMS;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using QDMSServer.DataSources;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public RealTimeDataBroker RealTimeBroker { get; set; }
        private readonly RealTimeDataServer _realTimeServer;
        public HistoricalDataBroker HistoricalBroker { get; set; }
        private readonly HistoricalDataServer _historicalDataServer;
        private readonly InstrumentsServer _instrumentsServer;

        private readonly IScheduler _scheduler;

        private readonly QDMSClient.QDMSClient _client;

        private ProgressBar _progressBar;

        public ObservableCollection<Instrument> Instruments { get; set; }

        public ObservableCollection<LogEventInfo> LogMessages { get; set; }

        public MainWindow()
        {
            Common.Logging.LogManager.Adapter = new NLogLoggerFactoryAdapter(new Common.Logging.Configuration.NameValueCollection());

            //make sure we can connect to the database
            CheckDBConnection();

            //set the log directory
            SetLogDirectory();

            //set the connection string
            DBUtils.SetConnectionString();

            InitializeComponent();
            DataContext = this;

            //load datagrid layout
            string layoutFile = AppDomain.CurrentDomain.BaseDirectory + "GridLayout.xml";
            if (File.Exists(layoutFile))
            {
                try
                {
                    InstrumentsGrid.DeserializeLayout(File.ReadAllText(layoutFile));
                }
                catch 
                {
                }
            }

            LogMessages = new ObservableCollection<LogEventInfo>();

            //target is where the log managers send their logs, here we grab the memory target which has a Subject to observe
            var target = LogManager.Configuration.AllTargets.Single(x => x.Name == "myTarget") as MemoryTarget;

            //we subscribe to the messages and send them all to the LogMessages collection
            if (target != null)
                target.Messages.Subscribe(msg => LogMessages.Add(msg));

            //build the instruments grid context menu
            //we want a button for each BarSize enum value in the UpdateFreqSubMenu menu
            foreach (int value in Enum.GetValues(typeof(BarSize)))
            {
                var button = new MenuItem
                {
                    Header = Regex.Replace(((BarSize)value).ToString(), "([A-Z])", " $1").Trim(),
                    Tag = (BarSize)value
                };
                button.Click += UpdateHistoricalDataBtn_ItemClick;
                ((MenuItem)Resources["UpdateFreqSubMenu"]).Items.Add(button);
            }

            //create metadata db if it doesn't exist
            var entityContext = new MyDBContext();
            entityContext.Database.Initialize(false);

            //seed the datasources no matter what, because these are added frequently
            Seed.SeedDatasources(entityContext);

            //check for any exchanges, seed the db with initial values if nothing is found
            if (!entityContext.Exchanges.Any())
            {
                Seed.DoSeed();
            }

            //create data db if it doesn't exist
            var dataContext = new DataDBContext();
            dataContext.Database.Initialize(false);
            dataContext.Dispose();

            //build the tags menu
            var allTags = entityContext.Tags.ToList();
            BuildTagContextMenu(allTags);

            //build session templates menu
            BuildSetSessionTemplateMenu();

            Instruments = new ObservableCollection<Instrument>();

            var mgr = new InstrumentManager();
            var instrumentList = mgr.FindInstruments(entityContext);

            foreach (Instrument i in instrumentList)
            {
                Instruments.Add(i);
            }

            //create brokers
            var cfRealtimeBroker = new ContinuousFuturesBroker(new QDMSClient.QDMSClient(
                "RTDBCFClient",
                "127.0.0.1",
                Properties.Settings.Default.rtDBReqPort,
                Properties.Settings.Default.rtDBPubPort,
                Properties.Settings.Default.instrumentServerPort,
                Properties.Settings.Default.hDBPort), new InstrumentManager(), connectImmediately: false);
            var cfHistoricalBroker = new ContinuousFuturesBroker(new QDMSClient.QDMSClient(
                "HDBCFClient",
                "127.0.0.1",
                Properties.Settings.Default.rtDBReqPort,
                Properties.Settings.Default.rtDBPubPort,
                Properties.Settings.Default.instrumentServerPort,
                Properties.Settings.Default.hDBPort), new InstrumentManager(), connectImmediately: false);
            var localStorage = DataStorageFactory.Get();
            RealTimeBroker = new RealTimeDataBroker(cfRealtimeBroker, localStorage,
                new IRealTimeDataSource[] {
                    //new Xignite(Properties.Settings.Default.xigniteApiToken),
                    //new Oanda(Properties.Settings.Default.oandaAccountId, Properties.Settings.Default.oandaAccessToken),
                    new IB(Properties.Settings.Default.rtdClientIBID),
                    //new ForexFeed(Properties.Settings.Default.forexFeedAccessKey, ForexFeed.PriceType.Mid)
                });
            HistoricalBroker = new HistoricalDataBroker(cfHistoricalBroker, localStorage,
                new IHistoricalDataSource[] {
                    new Yahoo(),
                    new FRED(),
                    //new Forexite(),
                    new IB(Properties.Settings.Default.histClientIBID),
                    new Quandl(Properties.Settings.Default.quandlAuthCode),
                });

            //create the various servers
            _realTimeServer = new RealTimeDataServer(Properties.Settings.Default.rtDBPubPort, Properties.Settings.Default.rtDBReqPort, RealTimeBroker);
            _instrumentsServer = new InstrumentsServer(Properties.Settings.Default.instrumentServerPort, mgr);
            _historicalDataServer = new HistoricalDataServer(Properties.Settings.Default.hDBPort, HistoricalBroker);

            //and start them
            _realTimeServer.StartServer();
            _instrumentsServer.StartServer();
            _historicalDataServer.StartServer();
            
            //we also need a client to make historical data requests with
            _client = new QDMSClient.QDMSClient(
                "SERVERCLIENT",
                "localhost",
                Properties.Settings.Default.rtDBReqPort,
                Properties.Settings.Default.rtDBPubPort,
                Properties.Settings.Default.instrumentServerPort,
                Properties.Settings.Default.hDBPort);
            _client.Connect();
            _client.HistoricalDataReceived += _client_HistoricalDataReceived;

            ActiveStreamGrid.ItemsSource = RealTimeBroker.ActiveStreams;

            //create the scheduler
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            _scheduler = schedulerFactory.GetScheduler();
            _scheduler.JobFactory = new DataUpdateJobFactory(HistoricalBroker,
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
                localStorage, new InstrumentManager());
            _scheduler.Start();

            //Grab jobs and schedule them
            JobsManager.ScheduleJobs(_scheduler, entityContext.DataUpdateJobs.Include(t => t.Instrument).Include(t => t.Tag).ToList());

            entityContext.Dispose();

            ShowChangelog();
        }

        private void ShowChangelog()
        {
            if (ApplicationDeployment.IsNetworkDeployed &&
                ApplicationDeployment.CurrentDeployment.IsFirstRun)
            {
                var window = new ChangelogWindow();
                window.Show();
            }
        }

        //creates a context menu to set tags on instruments
        private void BuildTagContextMenu(IEnumerable<Tag> tags)
        {
            var tagMenu = (MenuItem)Resources["InstrumentTagMenu"];
            tagMenu.Items.Clear();

            foreach (Tag t in tags)
            {
                var button = new MenuItem
                {
                    Header = t.Name,
                    Tag = t.ID,
                    IsCheckable = true,
                    Style = (Style)Resources["TagCheckStyle"]
                };

                button.Click += SetTag_ItemClick;
                tagMenu.Items.Add(button);
            }
            tagMenu.Items.Add(Resources["NewTagMenuItem"]);
        }

        private void SetLogDirectory()
        {
            if (Directory.Exists(Properties.Settings.Default.logDirectory))
            {
                ((FileTarget)LogManager.Configuration.FindTargetByName("default")).FileName = Properties.Settings.Default.logDirectory + "Log.log";
            }
        }

        private void CheckDBConnection()
        {
            //if no db type has been selected, we gotta show that window no matter what
            if (Properties.Settings.Default.databaseType != "MySql" && Properties.Settings.Default.databaseType != "SqlServer")
            {
                var dbDetailsWindow = new DBConnectionWindow();
                dbDetailsWindow.ShowDialog();
            }

            if (Properties.Settings.Default.databaseType == "MySql")
            {
                //try to establish a database connection. If not possible, prompt the user to enter details
                var connection = DBUtils.CreateMySqlConnection(noDB: true);
                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    var dbDetailsWindow = new DBConnectionWindow();
                    dbDetailsWindow.ShowDialog();
                }
                connection.Close();
            }
            else //SQL Server
            {
                //try to establish a database connection. If not possible, prompt the user to enter details
                var connection = DBUtils.CreateSqlServerConnection(noDB: true, useWindowsAuthentication: Properties.Settings.Default.sqlServerUseWindowsAuthentication);
                try
                {
                    connection.Open();
                }
                catch (Exception)
                {
                    var dbDetailsWindow = new DBConnectionWindow();
                    dbDetailsWindow.ShowDialog();
                }
                connection.Close();
            }
        }

        private void _client_HistoricalDataReceived(object sender, HistoricalDataEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
                {
                    _progressBar.Value++;
                    if (_progressBar.Value >= _progressBar.Maximum)
                    {
                        _progressBar.Value = 0;
                        _progressBar.Maximum = 0;
                        StatusBarLabel.Content = "Historical data update complete";
                    }
                    else
                    {
                        StatusBarLabel.Content = string.Format("Rcvd {0} bars of {1} @ {2}",
                            e.Data.Count,
                            e.Request.Instrument.Symbol,
                            e.Request.Frequency);
                    }
                }
                );
        }

        //check the latest date we have available in local storage, then request historical data from that date to the current time
        private void UpdateHistoricalDataBtn_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var frequency = (BarSize)((MenuItem)sender).Tag;
            List<Instrument> selectedInstruments = InstrumentsGrid.SelectedItems.Cast<Instrument>().ToList();

            int requestCount = 0;

            using (var localStorage = DataStorageFactory.Get())
            {
                foreach (Instrument i in selectedInstruments)
                {
                    if (!i.ID.HasValue) continue;

                    var storageInfo = localStorage.GetStorageInfo(i.ID.Value);
                    if (storageInfo.Any(x => x.Frequency == frequency))
                    {
                        var relevantStorageInfo = storageInfo.First(x => x.Frequency == frequency);
                        _client.RequestHistoricalData(new HistoricalDataRequest(
                            i,
                            frequency,
                            relevantStorageInfo.LatestDate + frequency.ToTimeSpan(),
                            DateTime.Now,
                            dataLocation: DataLocation.ExternalOnly,
                            saveToLocalStorage: true));
                        requestCount++;
                    }
                }
            }

            if (_progressBar.Value >= _progressBar.Maximum)
            {
                _progressBar.Maximum = requestCount;
                _progressBar.Value = 0;
            }
            else
            {
                _progressBar.Maximum += requestCount;
            }
        }

        //the application is closing, shut down all the servers and stuff
        private void DXWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //save grid layout
            using (StreamWriter file = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "GridLayout.xml"))
            {
                InstrumentsGrid.SerializeLayout(file);
            }

            //shut down quartz
            _scheduler.Shutdown(true);

            //then take down the client, the servers, and the brokers
            _client.Disconnect();
            _client.Dispose();

            _realTimeServer.StopServer();
            _realTimeServer.Dispose();

            _historicalDataServer.StopServer();
            _historicalDataServer.Dispose();

            _instrumentsServer.StopServer();
            _instrumentsServer.Dispose();

            RealTimeBroker.Dispose();

            HistoricalBroker.Dispose();
        }

        //exiting the application
        private void BtnExit_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            Close();
        }

        //show the interactive brokers add instrument window
        private void AddInstrumentIBBtn_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var window = new AddInstrumentInteractiveBrokersWindow();

            if (window.ViewModel != null && window.ViewModel.AddedInstruments != null)
            {
                foreach (Instrument i in window.ViewModel.AddedInstruments)
                {
                    Instruments.Add(i);
                }
                window.Close();
            }
        }

        //show the Quandl add instrument window
        private void AddInstrumentQuandlBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new AddInstrumentQuandlWindow();

            if (window.AddedInstruments != null)
            {
                foreach (Instrument i in window.AddedInstruments)
                {
                    Instruments.Add(i);
                }
                window.Close();
            }
        }

        //show the FRED add instrument window
        private void AddInstrumentFredBtn_OnClick(object sender, RoutedEventArgs e)
        {
            using (var context = new MyDBContext())
            {
                var window = new AddInstrumentFredWindow(context);

                if (window.AddedInstruments != null)
                {
                    foreach (Instrument i in window.AddedInstruments)
                    {
                        Instruments.Add(i);
                    }
                    window.Close();
                }
            }
        }

        //show a window to modify the selected instrument
        private void TableView_RowDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var inst = (Instrument)InstrumentsGrid.SelectedItem;
            var window = new AddInstrumentManuallyWindow(inst, false);
            window.ShowDialog();

            CollectionViewSource.GetDefaultView(InstrumentsGrid.ItemsSource).Refresh();

            window.Close();
        }

        //show the window to add a new custom futures contract
        private void BtnAddCustomFutures_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var window = new AddInstrumentManuallyWindow(addingContFut: true);
            window.ShowDialog();
            if (window.InstrumentAdded)
            {
                Instruments.Add(window.TheInstrument);
            }
            window.Close();
        }

        private void AddInstrumentManuallyBtn_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var window = new AddInstrumentManuallyWindow();
            window.ShowDialog();
            if (window.InstrumentAdded)
            {
                Instruments.Add(window.TheInstrument);
            }
            window.Close();
        }

        //clone an instrument
        private void InstrumentContextCloneBtn_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var inst = (Instrument)InstrumentsGrid.SelectedItem;
            var window = new AddInstrumentManuallyWindow(inst);
            window.ShowDialog();
            if (window.InstrumentAdded)
            {
                Instruments.Add(window.TheInstrument);
            }
            window.Close();
        }

        //delete one or more instruments
        private void DeleteInstrumentBtn_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var selectedInstruments = InstrumentsGrid.SelectedItems;
            if (selectedInstruments.Count == 0) return;

            if (selectedInstruments.Count == 1)
            {
                var inst = (Instrument)selectedInstruments[0];
                MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to delete {0} @ {1}?", inst.Symbol, inst.Datasource.Name),
                    "Delete", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No) return;
            }
            else
            {
                MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to delete {0} instruments?", selectedInstruments.Count),
                    "Delete", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No) return;
            }

            List<Instrument> toRemove = new List<Instrument>();

            foreach (Instrument i in InstrumentsGrid.SelectedItems)
            {
                InstrumentManager.RemoveInstrument(i, DataStorageFactory.Get());
                toRemove.Add(i);
            }

            while (toRemove.Count > 0)
            {
                Instruments.Remove(toRemove[toRemove.Count - 1]);
                toRemove.RemoveAt(toRemove.Count - 1);
            }
        }

        private void EditDataBtn_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var selectedInstruments = InstrumentsGrid.SelectedItems;
            if (selectedInstruments.Count != 1) return;

            var selectedInstrument = (Instrument)selectedInstruments[0];
            var window = new DataEditWindow(selectedInstrument);
            window.ShowDialog();
        }

        private void ImportDataBtn_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var selectedInstruments = InstrumentsGrid.SelectedItems;
            if (selectedInstruments.Count != 1) return;

            var selectedInstrument = (Instrument)selectedInstruments[0];
            var window = new DataImportWindow(selectedInstrument);
            window.ShowDialog();
        }

        private void ExchangesBtn_OnItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var window = new ExchangesWindow();
            window.ShowDialog();
        }

        private void SessionTemplateBtn_OnItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var window = new SessionTemplatesWindow();
            window.ShowDialog();
            BuildSetSessionTemplateMenu();
        }

        private void RootSymbolsBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new RootSymbolsWindow();
            window.ShowDialog();
        }

        private void PBar_Loaded(object sender, RoutedEventArgs e)
        {
            _progressBar = (ProgressBar)sender;
        }

        //delete data from selected instruments
        private void ClearDataBtn_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var selectedInstruments = InstrumentsGrid.SelectedItems;
            if (selectedInstruments.Count == 0) return;

            if (selectedInstruments.Count == 1)
            {
                var inst = (Instrument)selectedInstruments[0];
                MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to delete all data from {0} @ {1}?", inst.Symbol, inst.Datasource.Name),
                    "Delete", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No) return;
            }
            else
            {
                MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to delete all data from {0} instruments?", selectedInstruments.Count),
                    "Delete", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No) return;
            }

            using (var storage = DataStorageFactory.Get())
            {
                foreach (Instrument i in selectedInstruments)
                {
                    try
                    {
                        storage.DeleteAllInstrumentData(i);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }

            StatusBarLabel.Content = "Instrument data deleted";
        }

        //adds or removes a tag from one or more instruments
        private void SetTag_ItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            using (var context = new MyDBContext())
            {
                var selectedInstruments = InstrumentsGrid.SelectedItems;
                var btn = (MenuItem)routedEventArgs.Source;

                //one instrument selected
                foreach (Instrument instrument in selectedInstruments)
                {
                    context.Instruments.Attach(instrument);

                    if (btn.IsChecked)
                    {
                        var tag = context.Tags.First(x => x.ID == (int)btn.Tag);
                        context.Tags.Attach(tag);
                        instrument.Tags.Add(tag);
                    }
                    else
                    {
                        btn.IsChecked = false;
                        var tmpTag = instrument.Tags.First(x => x.ID == (int)btn.Tag);
                        context.Tags.Attach(tmpTag);
                        instrument.Tags.Remove(tmpTag);
                    }
                }

                context.SaveChanges();

                CollectionViewSource.GetDefaultView(InstrumentsGrid.ItemsSource).Refresh();
            }
        }

        private void BtnSettings_OnItemClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        //tag menu is opening, populate it with all tags and set the appropriate checkbox values
        private void InstrumentTagMenu_OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            List<Instrument> selectedInstruments = InstrumentsGrid.SelectedItems.Cast<Instrument>().ToList();
            if (selectedInstruments.Count == 0)
            {
                return;
            }
            else if (selectedInstruments.Count == 1)
            {
                var instrument = (Instrument)InstrumentsGrid.SelectedItem;
                //set checkboxes on the selected tags
                var instrumentTagMenu = (MenuItem)Resources["InstrumentTagMenu"];
                foreach (MenuItem btn in instrumentTagMenu.Items)
                {
                    if (btn.Tag == null || instrument.Tags == null) continue;

                    btn.IsChecked = instrument.Tags.Any(x => x.ID == (int)btn.Tag);
                    btn.IsEnabled = true;
                }
            }
            else
            {
                var instrumentTagMenu = (MenuItem)Resources["InstrumentTagMenu"];
                foreach (MenuItem btn in instrumentTagMenu.Items)
                {
                    if (btn.Tag == null) continue;

                    int tagCount = selectedInstruments.Count(x => x.Tags != null && x.Tags.Any(y => y.ID == (int)btn.Tag));
                    if (tagCount == 0 || tagCount == selectedInstruments.Count)
                    {
                        btn.IsEnabled = true;
                        btn.IsChecked = tagCount == selectedInstruments.Count;
                    }
                    else //if tags have different values among the selected instruments, just disable the button
                    {
                        btn.IsEnabled = false;
                    }
                }
            }
        }

        //add a new tag from the context menu and then apply it to the selected instruments
        private void NewTagTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var newTagTextBox = (TextBox)sender;

            using (var context = new MyDBContext())
            {
                string newTagName = newTagTextBox.Text;
                if (context.Tags.Any(x => x.Name == newTagName)) return; //tag already exists

                //add the tag
                var newTag = new Tag { Name = newTagName };
                context.Tags.Add(newTag);

                //apply the tag to the selected instruments
                var selectedInstruments = InstrumentsGrid.SelectedItems.Cast<Instrument>();
                foreach (Instrument i in selectedInstruments)
                {
                    context.Instruments.Attach(i);
                    i.Tags.Add(newTag);
                }

                context.SaveChanges();

                //update the tag menu
                var allTags = context.Tags.ToList();
                BuildTagContextMenu(allTags);
            }

            newTagTextBox.Text = "";

            CollectionViewSource.GetDefaultView(InstrumentsGrid.ItemsSource).Refresh();
        }

        private void NewDataRequestBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new HistoricalRequestWindow((Instrument)InstrumentsGrid.SelectedItem);
        }

        //enable/disable menuitems in the row context menu depending on what has been selected
        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //horrible, but what can you do?
            bool multipleSelected = InstrumentsGrid.SelectedItems.Count > 1;
            ContextMenu menu = (ContextMenu)Resources["RowMenu"];

            ((MenuItem)menu.Items[0]).IsEnabled = !multipleSelected; //new data request
            ((MenuItem)menu.Items[4]).IsEnabled = !multipleSelected; //clone
            ((MenuItem)menu.Items[5]).IsEnabled = !multipleSelected; //import data
            ((MenuItem)menu.Items[6]).IsEnabled = !multipleSelected; //edit data
        }

        private void BackupMetadataBtn_Click(object sender, RoutedEventArgs e)
        {
            DbBackup.Backup("qdmsEntities", "qdms");
        }

        private void BackupDataBtn_Click(object sender, RoutedEventArgs e)
        {
            DbBackup.Backup("qdmsDataEntities", "qdmsdata");
        }

        private void RestoreMetadataBtn_OnClick(object sender, RoutedEventArgs e)
        {
            DbBackup.Restore("qdmsEntities", "qdms");
        }

        private void RestoreDataBtn_OnClick(object sender, RoutedEventArgs e)
        {
            DbBackup.Restore("qdmsDataEntities", "qdmsdata");
        }

        private void DataJobsBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new ScheduledJobsWindow();
            window.ShowDialog();

            //clear and re-schedule all jobs, allowing any existing jobs to finish first.
            _scheduler.PauseAll();
            using (var entityContext = new MyDBContext())
            {
                JobsManager.ScheduleJobs(_scheduler, entityContext.DataUpdateJobs.Include(t => t.Instrument).Include(t => t.Tag).ToList());
            }

            var alljobs = _scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());

            _scheduler.ResumeAll();
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            var window = new AboutWindow();
            window.ShowDialog();
        }

        private void BuildSetSessionTemplateMenu()
        {
            var setSessionMenu = (MenuItem)Resources["InstrumentSetSessionMenu"];
            setSessionMenu.Items.Clear();

            using (var context = new MyDBContext())
            {
                foreach (SessionTemplate t in context.SessionTemplates.ToList())
                {
                    var button = new MenuItem
                    {
                        Header = t.Name,
                        Tag = t.ID,
                    };

                    button.Click += SetSession_ItemClick;
                    setSessionMenu.Items.Add(button);
                }
            }
        }

        private void SetSession_ItemClick(object sender, RoutedEventArgs e)
        {
            using (var context = new MyDBContext())
            {
                var selectedInstruments = InstrumentsGrid.SelectedItems;
                var btn = (MenuItem)e.Source;

                int templateID = (int)btn.Tag;

                var templateSessions = context.TemplateSessions.Where(x => x.TemplateID == templateID).ToList();

                //one instrument selected
                foreach (Instrument instrument in selectedInstruments)
                {
                    context.Instruments.Attach(instrument);
                    instrument.SessionsSource = SessionsSource.Template;
                    instrument.SessionTemplateID = templateID;

                    if(instrument.Sessions == null)
                    {
                        instrument.Sessions = new List<InstrumentSession>();
                    }

                    //Remove any old sessions
                    var tmpSessions = new List<InstrumentSession>(instrument.Sessions);
                    foreach (InstrumentSession isession in tmpSessions)
                    {
                        context.InstrumentSessions.Attach(isession);
                        context.InstrumentSessions.Remove(isession);
                    }

                    instrument.Sessions.Clear();

                    //Add the new sessions
                    foreach(TemplateSession ts in templateSessions)
                    {
                        instrument.Sessions.Add(ts.ToInstrumentSession());
                    }
                }

                context.SaveChanges();
            }
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            UpdateHelper.InstallUpdateSyncWithInfo();
        }
    }
}