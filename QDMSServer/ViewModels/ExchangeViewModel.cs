// -----------------------------------------------------------------------
// <copyright file="ExchangeViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMS.Server.Validation;
using ReactiveUI;

namespace QDMSServer.ViewModels
{
    public class ExchangeViewModel : ValidatingViewModelBase<Exchange>
    {
        private SessionViewModel _selectedSession;

        public ReactiveCommand<Unit, Unit> AddSession { get; set; }

        public ReactiveCommand<SessionViewModel, Unit> RemoveSession { get; set; }

        public ExchangeViewModel(Exchange model, IDataClient client, object parentVm, IDialogCoordinator dialogCoordinator) 
            : base(model, new ExchangeValidator())
        {
            var canExecuteSave = this.WhenAnyValue(x => x.HasErrors, x => !x);
            Save = ReactiveCommand.CreateFromTask(async _ =>
                {
                    var result = await client.UpdateExchange(Model).ConfigureAwait(true);
                    await result.DisplayErrors(parentVm, dialogCoordinator).ConfigureAwait(true);
                },
                canExecuteSave);

            var canRemove = this.WhenAnyValue(x => x.SelectedSession).Select(x => x != null);
            RemoveSession = ReactiveCommand.Create<SessionViewModel>(viewModel =>
                {
                    Sessions.Remove(viewModel);
                    Model.Sessions.Remove((ExchangeSession)viewModel.Model);
                },
                canRemove);

            AddSession = ReactiveCommand.Create(() =>
            {
                var toAdd = new ExchangeSession { IsSessionEnd = true };

                if (Sessions.Count == 0)
                {
                    toAdd.OpeningDay = DayOfTheWeek.Monday;
                    toAdd.ClosingDay = DayOfTheWeek.Monday;
                }
                else
                {
                    DayOfTheWeek maxDay = (DayOfTheWeek)Math.Min(6, Sessions.Max(x => (int)x.OpeningDay) + 1);
                    toAdd.OpeningDay = maxDay;
                    toAdd.ClosingDay = maxDay;
                }
                Sessions.Add(new SessionViewModel(toAdd));
                Model.Sessions.Add(toAdd);
            });

            //populate session collection
            if (model.Sessions == null)
            {
                model.Sessions = new List<ExchangeSession>();
            }

            foreach (var session in model.Sessions)
            {
                Sessions.Add(new SessionViewModel(session));
            }
        }

        public ReactiveCommand<Unit, Unit> Save { get; set; }

        public int ID => Model.ID;

        public string Name
        {
            get => Model.Name;
            set
            {
                if (value == Model.Name) return;
                Model.Name = value;
                this.RaisePropertyChanged();
            }
        }

        public string LongName
        {
            get => Model.LongName;
            set
            {
                if (value == Model.LongName) return;
                Model.LongName = value;
                this.RaisePropertyChanged();
            }
        }

        public string Timezone
        {
            get => Model.Timezone;
            set
            {
                if (value == Model.Timezone) return;
                Model.Timezone = value;
                this.RaisePropertyChanged();
            }
        }

        public ReactiveList<SessionViewModel> Sessions { get; set; } = new ReactiveList<SessionViewModel>();

        public SessionViewModel SelectedSession
        {
            get => _selectedSession;
            set => this.RaiseAndSetIfChanged(ref _selectedSession, value);
        }
    }
}
