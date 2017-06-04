// -----------------------------------------------------------------------
// <copyright file="AddInsturmentQuandlViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls.Dialogs;
using NLog;
using QDMS;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace QDMSServer.ViewModels
{
    public class AddInstrumentQuandlViewModel : ReactiveObject
    {
        private readonly IDataClient _client;
        private readonly IDialogCoordinator _dialogCoordinator;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private int? _currentPage;
        private string _status;
        private string _symbol;
        private Datasource _thisDS;
        public AddInstrumentQuandlViewModel(IDataClient client, IDialogCoordinator dialogCoordinator, string authToken)
        {
            _client = client;
            _dialogCoordinator = dialogCoordinator;

            //Create commands
            Load = ReactiveCommand.CreateFromTask(async _ =>
            {
                var dsResult = await _client.GetDatasources().ConfigureAwait(true);
                if (dsResult.WasSuccessful)
                {
                    _thisDS = dsResult.Result.FirstOrDefault(x => x.Name == "Quandl");
                }
                else
                {
                    _logger.Error("Could not find FRED datasource");
                }
            });

            var canSearch = this.WhenAnyValue(x => x.Symbol).Select(x => !string.IsNullOrEmpty(x));
            Search = ReactiveCommand.CreateFromTask(async _ =>
            {
                Status = "Searching...";

                Instruments.Clear();
                QuandlUtils.QuandlInstrumentSearchResult foundInstruments;
                try
                {
                    foundInstruments = await QuandlUtils.FindInstruments(Symbol, authToken, CurrentPage ?? 1).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    await _dialogCoordinator.ShowMessageAsync(this, "Error", ex.Message).ConfigureAwait(true);
                    return;
                }

                foreach (var i in foundInstruments.Instruments)
                {
                    i.Datasource = _thisDS;
                    i.DatasourceID = _thisDS.ID;
                    i.Multiplier = 1;
                    Instruments.Add(i);
                }

                Status = foundInstruments.Count + " contracts found";

                CurrentPage = CurrentPage ?? 1;
            }, canSearch);

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

            this.WhenAny(x => x.CurrentPage, x => x)
                .Select(x => new Unit()) //hack
                .InvokeCommand(Search);

            IncrementPage = ReactiveCommand.Create(() => CurrentPage == null ? 1 : CurrentPage++);
            DecrementPage = ReactiveCommand.Create(() => Math.Max(1, CurrentPage == null ? 1 : (int)CurrentPage--));
        }

        public ReactiveCommand<IList, Unit> Add { get; set; }
        public List<Instrument> AddedInstruments { get; set; } = new List<Instrument>();
        public int? CurrentPage
        {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public ReactiveCommand<Unit, int> DecrementPage { get; set; }
        public ReactiveCommand<Unit, int?> IncrementPage { get; set; }
        public ReactiveList<Instrument> Instruments { get; set; } = new ReactiveList<Instrument>();
        public ReactiveCommand<Unit, Unit> Load { get; set; }

        public ReactiveCommand<Unit, Unit> Search { get; set; }

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