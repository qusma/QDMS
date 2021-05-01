// -----------------------------------------------------------------------
// <copyright file="UnderlyingSymbolsViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls.Dialogs;
using QDMS;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace QDMSApp.ViewModels
{
    public class UnderlyingSymbolsViewModel : ReactiveObject
    {
        private UnderlyingSymbolViewModel _selectedSymbol;
        private IDisposable _selectedSymbolHasErrorsSubscription;

        /// <summary>
        /// Design-time use only
        /// </summary>
        [Obsolete]
        public UnderlyingSymbolsViewModel() { }

        public UnderlyingSymbolsViewModel(IDataClient client, IDialogCoordinator dialogCoordinator)
        {
            UnderlyingSymbols = new ObservableCollection<UnderlyingSymbolViewModel>();

            //Load the symbols

            Load = ReactiveCommand.CreateFromTask(async _ =>
            {
                var result = await client.GetUnderlyingSymbols();

                if (await result.DisplayErrors(this, dialogCoordinator)) return null;

                return result.Result;
            });

            Load.Subscribe(symbols =>
            {
                if (symbols == null) return;

                foreach (var vm in symbols.Select(x => new UnderlyingSymbolViewModel(x)))
                {
                    UnderlyingSymbols.Add(vm);
                }
            });

            //Add

            Add = ReactiveCommand.CreateFromTask(async _ =>
            {
                var symbolName = await dialogCoordinator.ShowInputAsync(this, "Symbol Name", "Enter new symbol name");
                if (string.IsNullOrEmpty(symbolName)) return null;

                var newSymbol = new UnderlyingSymbol { Symbol = symbolName, Rule = new ExpirationRule() };

                var response = await client.AddUnderlyingSymbol(newSymbol);
                if (await response.DisplayErrors(this, dialogCoordinator)) return null;

                return response.Result;
            });

            Add.Where(x => x != null)
                .Subscribe(underlyingSymbol =>
            {
                var vm = new UnderlyingSymbolViewModel(underlyingSymbol);
                UnderlyingSymbols.Add(vm);
                SelectedSymbol = vm;
            });

            //Delete

            var deleteCanExecute = this
                .WhenAnyValue(x => x.SelectedSymbol)
                .Select(x => x != null);
            Delete = ReactiveCommand.CreateFromTask(async _ =>
            {
                var sureDelete = await dialogCoordinator.ShowMessageAsync(this, "Delete",
                    $"Are you sure you want to delete {SelectedSymbol.Symbol}?", MessageDialogStyle.AffirmativeAndNegative);
                if (sureDelete == MessageDialogResult.Negative) return;

                var response = await client.DeleteUnderlyingSymbol(SelectedSymbol.Model);
                if (await response.DisplayErrors(this, dialogCoordinator)) return;

                UnderlyingSymbols.Remove(SelectedSymbol);
                SelectedSymbol = null;
            },
            deleteCanExecute);

            //Save
            var saveCanExecute = this
                .WhenAnyValue(x => x.SelectedSymbol, x => x.SelectedSymbolHasErrors, (symbol, hasError) => new { symbol, hasError })
                .Select(x => x.symbol != null && x.hasError == false);
            Save = ReactiveCommand.CreateFromTask(async _ =>
            {
                var response = await client.UpdateUnderlyingSymbol(SelectedSymbol.Model);
                await response.DisplayErrors(this, dialogCoordinator);
            },
            saveCanExecute);
        }

        public ReactiveCommand<Unit, Unit> Save { get; set; }

        public UnderlyingSymbolViewModel SelectedSymbol
        {
            get { return _selectedSymbol; }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSymbol, value);

                //update the SelectedSymbolHasErrors subscription
                _selectedSymbolHasErrorsSubscription?.Dispose();
                if (_selectedSymbol == null) return;
                _selectedSymbolHasErrorsSubscription = _selectedSymbol
                    .WhenAnyValue(x => x.HasErrors)
                    .Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedSymbolHasErrors)));
            }
        }

        public ObservableCollection<UnderlyingSymbolViewModel> UnderlyingSymbols { get; }
        public ReactiveCommand<Unit, List<UnderlyingSymbol>> Load { get; }
        public ReactiveCommand<Unit, UnderlyingSymbol> Add { get; }
        public ReactiveCommand<Unit, Unit> Delete { get; }

        /// <summary>
        /// Conveniently passes through HasErrors for the selected symbol
        /// </summary>
        public bool SelectedSymbolHasErrors => SelectedSymbol?.HasErrors ?? false;
    }
}