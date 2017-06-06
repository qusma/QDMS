// -----------------------------------------------------------------------
// <copyright file="ExchangesViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using DynamicData;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData.Binding;

namespace QDMSServer.ViewModels
{
    public class ExchangesViewModel : ReactiveObject, IDisposable
    {
        private SourceCache<ExchangeViewModel, int> _allExchanges = new SourceCache<ExchangeViewModel, int>(x => x.ID);
        private IObservable<Func<ExchangeViewModel, bool>> _filterPredicate;

        public ReadOnlyObservableCollection<ExchangeViewModel> Exchanges => _exchanges;

        public ExchangeViewModel SelectedExchange
        {
            get => _selectedExchange;
            set => this.RaiseAndSetIfChanged(ref _selectedExchange, value);
        }

        public ReactiveList<TimeZoneInfo> Timezones { get; } = new ReactiveList<TimeZoneInfo>();

        private string _searchTerm;
        private readonly ReadOnlyObservableCollection<ExchangeViewModel> _exchanges;
        private IDisposable _filterOp;
        private ExchangeViewModel _selectedExchange;

        public string SearchTerm
        {
            get => _searchTerm;
            set => this.RaiseAndSetIfChanged(ref _searchTerm, value);
        }

        public ExchangesViewModel(IDataClient client, IDialogCoordinator dialogCoordinator)
        {
            Load = ReactiveCommand.CreateFromTask(async _ =>
            {
                var result = await client.GetExchanges().ConfigureAwait(true);
                if (await result.DisplayErrors(this, dialogCoordinator).ConfigureAwait(true))
                {
                    return;
                }

                foreach (var exchange in result.Result)
                {
                    _allExchanges.AddOrUpdate(new ExchangeViewModel(exchange, client, this, dialogCoordinator));
                }
            });

            Add = ReactiveCommand.CreateFromTask(async _ =>
            {
                var newExchange = new Exchange { Name = "NewExchange", LongName = "New Exchange", Timezone = "Eastern Standard Time" };
                var addResult = await client.AddExchange(newExchange).ConfigureAwait(true);
                if (await addResult.DisplayErrors(this, dialogCoordinator).ConfigureAwait(true))
                {
                    return;
                }

                var vm = new ExchangeViewModel(addResult.Result, client, this, dialogCoordinator);
                _allExchanges.AddOrUpdate(vm);

                SelectedExchange = vm;
            });

            var canDelete = this.WhenAnyValue(x => x.SelectedExchange)
                .Select(x => x != null);
            Delete = ReactiveCommand.CreateFromTask(async _ =>
            {
                var confirm = await dialogCoordinator.ShowMessageAsync(this, "Delete", $"Are you sure you want to delete {SelectedExchange.LongName}?", MessageDialogStyle.AffirmativeAndNegative).ConfigureAwait(true);
                if (confirm == MessageDialogResult.Negative) return;

                var result = await client.DeleteExchange(SelectedExchange.Model).ConfigureAwait(true);
                if (await result.DisplayErrors(this, dialogCoordinator).ConfigureAwait(true))
                {
                    return;
                }

                _allExchanges.RemoveKey(SelectedExchange.ID);
            }, canDelete);

            //Populate timezones
            var timezones = TimeZoneInfo.GetSystemTimeZones();
            foreach (TimeZoneInfo tz in timezones)
            {
                Timezones.Add(tz);
            }

            //Set up search functionality
            _filterPredicate = this.WhenAnyValue(x => x.SearchTerm)
                .Throttle(TimeSpan.FromMilliseconds(25))
                .Select(x => x?.ToLower())
                .Select(search =>
                    new Func<ExchangeViewModel, bool>(
                        exch => string.IsNullOrEmpty(search)
                            || (exch.Name != null && exch.Name.ToLower().Contains(search))
                            || (exch.LongName != null && exch.LongName.ToLower().Contains(search)))
                );

            _filterOp = _allExchanges.Connect()
                .Filter(_filterPredicate)
                .Sort(SortExpressionComparer<ExchangeViewModel>.Ascending(t => t.Name))
                .ObserveOnDispatcher()
                .Bind(out _exchanges)
                .DisposeMany()
                .Subscribe();
        }

        public ReactiveCommand<Unit, Unit> Add { get; set; }

        public ReactiveCommand<Unit, Unit> Delete { get; set; }

        public ReactiveCommand<Unit, Unit> Load { get; set; }

        public void Dispose()
        {
            _allExchanges?.Dispose();
            Load?.Dispose();
            _filterOp?.Dispose();
        }
    }
}