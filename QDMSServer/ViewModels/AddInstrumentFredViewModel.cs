// -----------------------------------------------------------------------
// <copyright file="AddInstrumentFredViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using QDMS;
using ReactiveUI;

namespace QDMSServer.ViewModels
{
    public class AddInstrumentFredViewModel : ReactiveObject
    {
        private Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IDataClient _client;
        private readonly IDialogCoordinator _dialogCoordinator;
        private string _status;
        private Datasource _thisDS;
        private string _symbol;
        private const string ApiKey = "f8d71bdcf1d7153e157e0baef35f67db";

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

        public ReactiveCommand<string, Unit> Search { get; }

        public ReactiveList<FredUtils.FredSeries> Series { get; }
        public List<Instrument> AddedInstruments { get; set; } = new List<Instrument>();

        public AddInstrumentFredViewModel(IDataClient client, IDialogCoordinator dialogCoordinator)
        {
            _client = client;
            _dialogCoordinator = dialogCoordinator;
            Series = new ReactiveList<FredUtils.FredSeries>();

            //Create commands
            Load = ReactiveCommand.CreateFromTask(async _ =>
            {
                var dsResult = await _client.GetDatasources().ConfigureAwait(true);
                if (dsResult.WasSuccessful)
                {
                    _thisDS = dsResult.Result.FirstOrDefault(x => x.Name == "FRED");
                }
                else
                {
                    _logger.Error("Could not find FRED datasource");
                }
            });

            var canSearch = this.WhenAnyValue(x => x.Symbol).Select(x => !string.IsNullOrEmpty(x));
            Search = ReactiveCommand.CreateFromTask<string>(async symbol =>
            {
                Series.Clear();
                Status = "Searching...";
                try
                {
                    var foundSeries = await FredUtils.FindSeries(Symbol, ApiKey).ConfigureAwait(true);
                    foreach (var i in foundSeries)
                    {
                        Series.Add(i);
                    }

                    Status = foundSeries.Count() + " contracts found";
                }
                catch(Exception ex)
                {
                    await _dialogCoordinator.ShowMessageAsync(this, "Error", ex.Message).ConfigureAwait(true);
                }
            }, canSearch);

            Add = ReactiveCommand.CreateFromTask<IList>(async selectedInstruments =>
            {
                int count = 0;

                foreach (FredUtils.FredSeries series in selectedInstruments)
                {
                    var newInstrument = FredUtils.SeriesToInstrument(series);
                    newInstrument.Datasource = _thisDS;
                    newInstrument.DatasourceID = _thisDS.ID;

                    var result = await _client.AddInstrument(newInstrument).ConfigureAwait(true);
                    if (await result.DisplayErrors(this, _dialogCoordinator).ConfigureAwait(true))
                    {
                        continue;
                    }

                    count++;
                    AddedInstruments.Add(result.Result);
                }

                Status = string.Format("{0}/{1} instruments added.", count, selectedInstruments.Count);
            });

        }

        public ReactiveCommand<Unit, Unit> Load { get; set; }

        public ReactiveCommand<IList, Unit> Add { get; set; }
    }
}
