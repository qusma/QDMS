// -----------------------------------------------------------------------
// <copyright file="AddInstrumentIbViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using EntityData;
using Krs.Ats.IBNet;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using QDMS;
using ReactiveUI;

namespace QDMSServer.ViewModels
{
    public class AddInstrumentIbViewModel : ReactiveObject, IDisposable
    {
        public AddInstrumentIbViewModel(IDialogCoordinator dialogService, IDataClient client)
        {
            _dialogService = dialogService;
            _qdmsClient = client;
            CreateCommands();

            Random r = new Random();
            _client = new IBClient();

            //random connection id for this one...
            _client.Connect(Properties.Settings.Default.ibClientHost, Properties.Settings.Default.ibClientPort, r.Next(1000, 200000));

            AddedInstruments = new List<Instrument>();

            _client.ContractDetails += _client_ContractDetails;
            _client.ContractDetailsEnd += _client_ContractDetailsEnd;

            Observable
                .FromEventPattern<ConnectionClosedEventArgs>(_client, "ConnectionClosed")
                .Subscribe(e => _logger.Warn("IB Instrument Adder connection closed."));

            Observable
                .FromEventPattern<NextValidIdEventArgs>(_client, "NextValidId")
                .Subscribe(e => _nextRequestID = e.EventArgs.OrderId);

            Observable
                .FromEventPattern<ErrorEventArgs>(_client, "Error")
                .Subscribe(e =>
                {
                    if (e.EventArgs.ErrorMsg != "No security definition has been found for the request")
                    {
                        _logger.Error($"{e.EventArgs.ErrorCode} - {e.EventArgs.ErrorMsg}");
                    }

                    Status = e.EventArgs.ErrorMsg;
                    SearchUnderway = false;
                });

            Exchanges = new ObservableCollection<string> { "All" };
            _exchanges = new Dictionary<string, Exchange>();

            using (var context = new MyDBContext())
            {
                _thisDS = context.Datasources.First(x => x.Name == "Interactive Brokers");

                foreach (Exchange e in context.Exchanges.Include(x => x.Sessions))
                {
                    Exchanges.Add(e.Name);
                    _exchanges.Add(e.Name, e);
                }
            }

            Instruments = new ObservableCollection<Instrument>();
            InstrumentTypes = new ObservableCollection<InstrumentType>();

            //list the available types from our enum
            var values = MyUtils.GetEnumValues<InstrumentType>();
            foreach (var val in values)
            {
                InstrumentTypes.Add(val);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private void _client_BatchContractDetail(object sender, ContractDetailsEventArgs e)
        {
            Status = string.Format("{0}/{1} symbols received", _totalSymbols - _queuedSymbols.Count, _totalSymbols);
            _tmpContractDetails.Add(e);
        }

        private async void _client_BatchContractDetailsEnd(object sender, ContractDetailsEndEventArgs e)
        {
            if (_tmpContractDetails.Count == 1) //we only want one because otherwise there is ambiguity in the contracts
            {
                Instrument instrument = ContractToInstrument(_tmpContractDetails[0]);

                if (instrument != null && await TryAddInstrument(instrument).ConfigureAwait(true) != null)
                {
                    //successfully added the symbol
                    _symbolsAdded.Add(instrument.Symbol);
                }
            }
            else
            {
                _logger.Info("Could not batch add " + _tmpContractDetails.FirstOrDefault()?.ContractDetails.Summary.Symbol + ", " + _tmpContractDetails.Count + " ambiguous contracts found.");
            }

            _tmpContractDetails.Clear();

            if (_queuedSymbols.Count == 0)
            {
                //in this case, we have completed all the requests
                BatchRequestJobCompleted();
            }
            else
            {
                //we're not done, send the next request
                SendNextRequestInBatch();
            }
        }

        private void _client_ContractDetails(object sender, ContractDetailsEventArgs e)
        {
            Instrument instrument = ContractToInstrument(e);
            if (instrument == null) return;

            Application.Current.Dispatcher.Invoke(() => Instruments.Add(instrument));
        }

        void _client_ContractDetailsEnd(object sender, ContractDetailsEndEventArgs e)
        {
            SearchUnderway = false; //re-enables the search commands
            Status = Instruments.Count + " contracts arrived";
        }

        private void BatchRequestJobCompleted()
        {
            Status = string.Format("Batch addition complete: {0} of {1} successfully added",
                _symbolsAdded.Count, _totalSymbols);

            SearchUnderway = false; //re-enables the search commands
            _client.ContractDetails -= _client_BatchContractDetail;
            _client.ContractDetails += _client_ContractDetails;
            _client.ContractDetailsEnd -= _client_BatchContractDetailsEnd;
            _client.ContractDetailsEnd += _client_ContractDetailsEnd;
            _client.Error -= _client_BatchAddingError;

            //some symbols may not have been added, we put them back in the textbox so the user can see which
            MultiSymbolText = string.Join(", ", _batchAllSymbols.Except(_symbolsAdded));
        }

        private Instrument ContractToInstrument(ContractDetailsEventArgs e)
        {
            var instrument = TWSUtils.ContractDetailsToInstrument(e.ContractDetails);
            instrument.Datasource = _thisDS;
            instrument.DatasourceID = _thisDS.ID;
            if (e.ContractDetails.Summary.Exchange != null && _exchanges.ContainsKey(e.ContractDetails.Summary.Exchange))
            {
                instrument.Exchange = _exchanges[e.ContractDetails.Summary.Exchange];
                instrument.ExchangeID = instrument.Exchange.ID;
            }
            else
            {
                _logger.Error("Could not find exchange in database: " + e.ContractDetails.Summary.Exchange);
                return null;
            }

            if (e.ContractDetails.Summary.PrimaryExchange != null &&
                _exchanges.ContainsKey(e.ContractDetails.Summary.PrimaryExchange))
            {
                instrument.PrimaryExchange = _exchanges[e.ContractDetails.Summary.PrimaryExchange];
                instrument.PrimaryExchangeID = instrument.PrimaryExchange.ID;
            }
            else if (!string.IsNullOrEmpty(e.ContractDetails.Summary.PrimaryExchange))
            {
                _logger.Error("Could not find exchange in database: " + e.ContractDetails.Summary.PrimaryExchange);
                return null;
            }
            return instrument;
        }

        private void CreateCommands()
        {
            AddSelectedInstruments = ReactiveCommand.CreateFromTask<IList>(async instruments => 
                await ExecuteAddSelectedInstruments(instruments).ConfigureAwait(true));

            Search = ReactiveCommand.Create(ExecuteSearch, this.WhenAny(x => x.SearchUnderway, x => !x.Value).ObserveOnDispatcher());

            BatchAddMultipleSymbols = ReactiveCommand.Create(ExecuteBatchAddSymbols, this.WhenAny(x => x.SearchUnderway, x => !x.Value).ObserveOnDispatcher());
        }

        private async Task ExecuteAddSelectedInstruments(IList selectedInstruments)
        {
            if (selectedInstruments == null) throw new ArgumentNullException(nameof(selectedInstruments));
            if (selectedInstruments.Count == 0) return;

            int count = 0;
            foreach (Instrument newInstrument in selectedInstruments)
            {
                if (await TryAddInstrument(newInstrument).ConfigureAwait(true) != null)
                {
                    count++;
                }
            }

            Status = string.Format("{0}/{1} instruments added.", count, selectedInstruments.Count);
        }

        private void ExecuteBatchAddSymbols()
        {
            if (string.IsNullOrEmpty(MultiSymbolText)) return;
            List<string> symbols = MultiSymbolText
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            if (symbols.Count == 0) return;
            _totalSymbols = symbols.Count;

            SearchUnderway = true; //disables the search commands

            //we set up a queue and progressively empty it by querying the symbols one by one and adding what we can
            _client.ContractDetails += _client_BatchContractDetail;
            _client.ContractDetails -= _client_ContractDetails;
            _client.ContractDetailsEnd += _client_BatchContractDetailsEnd;
            _client.ContractDetailsEnd -= _client_ContractDetailsEnd;
            _client.Error += _client_BatchAddingError;

            _symbolsAdded.Clear();
            _batchAllSymbols.Clear();
            _batchAllSymbols.AddRange(symbols);

            foreach (string s in symbols)
            {
                _queuedSymbols.Enqueue(s);
            }

            //start the process of sending the queries
            SendNextRequestInBatch();
        }

        private void _client_BatchAddingError(object sender, ErrorEventArgs e)
        {
            if (e.ErrorMsg == "No security definition has been found for the request")
            {
                SendNextRequestInBatch();
            }
        }

        private void ExecuteSearch()
        {
            Instruments.Clear();
            SendContractDetailsRequest(Symbol);
        }

        private void SendContractDetailsRequest(string symbol)
        {
            var contract = new Contract
            {
                Symbol = symbol,
                SecurityType = TWSUtils.SecurityTypeConverter(SelectedType),
                Exchange = SelectedExchange == "All" ? "" : SelectedExchange,
                IncludeExpired = IncludeExpired,
                Currency = Currency
            };

            if (ExpirationDate.HasValue)
                contract.Expiry = ExpirationDate.Value.ToString("yyyyMM");

            if(Strike.HasValue)
            {
                contract.Strike = Strike.Value;
            }

            SearchUnderway = true; //disables the search commands
            _client.RequestContractDetails(_nextRequestID, contract);
        }

        /// <summary>
        /// When doing a batch symbol addition, this method will send successive requests for the contract details
        /// </summary>
        private void SendNextRequestInBatch()
        {
            if (_queuedSymbols.Count == 0) return;
            string nextSymbol = _queuedSymbols.Dequeue();
            SendContractDetailsRequest(nextSymbol);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instrument"></param>
        /// <returns>null if addition failed</returns>
        private async Task<Instrument> TryAddInstrument(Instrument instrument)
        {
            var result = await _qdmsClient.AddInstrument(instrument).ConfigureAwait(true);
            if (await result.DisplayErrors(this, _dialogService).ConfigureAwait(true))
            {
                //request failed
                _logger.Error("IB add instrument failure: " + string.Join(",", result.Errors));
                return null;
            }

            var addedInstrument = result.Result;
            AddedInstruments.Add(addedInstrument);
            return addedInstrument;
        }

        public List<Instrument> AddedInstruments { get; }

        public ReactiveCommand<IList, Unit> AddSelectedInstruments { get; private set; }

        public ReactiveCommand<Unit, Unit> BatchAddMultipleSymbols { get; private set; }

        public string Currency
        {
            get => _currency;
            set => this.RaiseAndSetIfChanged(ref _currency, value);
        }

        public ObservableCollection<string> Exchanges { get; set; }

        public DateTime? ExpirationDate
        {
            get => _expirationDate;
            set => this.RaiseAndSetIfChanged(ref _expirationDate, value);
        }

        public bool IncludeExpired
        {
            get => _includeExpired;
            set => this.RaiseAndSetIfChanged(ref _includeExpired, value);
        }

        public ObservableCollection<Instrument> Instruments { get; set; }

        public ObservableCollection<InstrumentType> InstrumentTypes { get; set; }

        /// <summary>
        /// Used to add multiple symbols in a batch.
        /// </summary>
        public string MultiSymbolText
        {
            get => _multiSymbolText;
            set => this.RaiseAndSetIfChanged(ref _multiSymbolText, value);
        }

        public ReactiveCommand<Unit, Unit> Search { get; private set; }

        public bool SearchUnderway
        {
            get => _searchUnderway;
            private set => this.RaiseAndSetIfChanged(ref _searchUnderway, value);
        }

        public string SelectedExchange
        {
            get => _selectedExchange;
            set => this.RaiseAndSetIfChanged(ref _selectedExchange, value);
        }

        public InstrumentType SelectedType
        {
            get => _selectedType;
            set => this.RaiseAndSetIfChanged(ref _selectedType, value);
        }

        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public double? Strike
        {
            get => _strike;
            set => this.RaiseAndSetIfChanged(ref _strike, value);
        }

        public string Symbol
        {
            get => _symbol;
            set => this.RaiseAndSetIfChanged(ref _symbol, value);
        }

        private readonly IBClient _client;
        private readonly IDataClient _qdmsClient;
        private readonly IDialogCoordinator _dialogService;
        private readonly Dictionary<string, Exchange> _exchanges;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Used to hold the symbols when doing batch additions
        /// </summary>
        private readonly Queue<string> _queuedSymbols = new Queue<string>();
        /// <summary>
        /// When doing a batch addition, symbols successfully added are kept here
        /// </summary>
        private readonly List<string> _symbolsAdded = new List<string>();

        /// <summary>
        /// When doing batch addition, all symbols being processed are held here.
        /// </summary>
        private readonly List<string> _batchAllSymbols = new List<string>();

        private readonly Datasource _thisDS;

        /// <summary>
        /// Holds ContractDetailsEventArgs which are then processed in _client_BatchContractDetailsEnd.
        /// Can't process them one by one because we need there to only be one.
        /// </summary>
        private readonly List<ContractDetailsEventArgs> _tmpContractDetails = new List<ContractDetailsEventArgs>();

        private string _currency;

        private DateTime? _expirationDate;

        private bool _includeExpired;

        private int _nextRequestID;

        private bool _searchUnderway;

        private string _selectedExchange;

        private InstrumentType _selectedType;

        private string _status;

        private double? _strike;

        private string _symbol;

        /// <summary>
        /// Holds the total number of symbols to be done in a batch process
        /// </summary>
        private int _totalSymbols = 0;

        private string _multiSymbolText;
    }
}