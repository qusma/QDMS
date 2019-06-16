// -----------------------------------------------------------------------
// <copyright file="AddInstrumentBinanceViewModel.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using QDMS;
using QDMS.Server.DataSources.Binance;
using ReactiveUI;
using ReactiveUI.Legacy;

namespace QDMSServer.ViewModels
{
    public class AddInstrumentBinanceViewModel : ReactiveObject
    {
        private readonly IDataClient _client;
        private IDialogCoordinator _dialogCoordinator;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private string _symbol;
        private List<Instrument> _allInstruments = new List<Instrument>();
        private Datasource _thisDS;
        private string _status;

        public AddInstrumentBinanceViewModel(IDataClient client, IDialogCoordinator dialogCoordinator)
        {
            _client = client;
            _dialogCoordinator = dialogCoordinator;

            //Create commands
            Load = ReactiveCommand.CreateFromTask(async _ =>
            {
                //load datasource
                var dsResult = await _client.GetDatasources().ConfigureAwait(true);
                if (dsResult.WasSuccessful)
                {
                    _thisDS = dsResult.Result.FirstOrDefault(x => x.Name == "Binance");
                }
                else
                {
                    _logger.Error("Could not find Binance datasource");
                    return;
                }

                //load instruments
                try
                {
                    var instruments = await BinanceUtils.GetInstruments(_thisDS);
                    foreach (var inst in instruments)
                    {
                        Instruments.Add(inst);
                        _allInstruments.Add(inst);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Could not load symbols from binance");
                }
            });

            Search = ReactiveCommand.Create(() =>
            {
                Instruments.Clear();
                foreach (var i in _allInstruments.Where(x => string.IsNullOrEmpty(Symbol) || //case-insensitive Contains()
                                                             x.Symbol.IndexOf(Symbol, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    Instruments.Add(i);
                }
            });

            Add = ReactiveCommand.CreateFromTask<IList>(async selectedInstruments =>
            {
                int count = 0;

                foreach (Instrument instrument in selectedInstruments)
                {
                    instrument.Datasource = _thisDS;
                    instrument.DatasourceID = _thisDS.ID;

                    var result = await _client.AddInstrument(instrument).ConfigureAwait(true);
                    if (await result.DisplayErrors(this, _dialogCoordinator).ConfigureAwait(true))
                    {
                        continue;
                    }

                    count++;
                    AddedInstruments.Add(result.Result);
                }

                Status = string.Format("{0}/{1} instruments added.", count, selectedInstruments.Count);
            });

            this.WhenAny(x => x.Symbol, x => x)
                .Select(x => new Unit()) //hack
                .InvokeCommand(Search);
        }

        public ReactiveCommand<IList, Unit> Add { get; set; }

        public ReactiveCommand<Unit, Unit> Search { get; set; }

        public ReactiveCommand<Unit, Unit> Load { get; set; }

        public ObservableCollection<Instrument> Instruments { get; set; } = new ObservableCollection<Instrument>();
        public List<Instrument> AddedInstruments { get; set; } = new List<Instrument>();
        public string Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public string Symbol
        {
            get => _symbol;
            set => this.RaiseAndSetIfChanged(ref _symbol, value);
        }
    }
}