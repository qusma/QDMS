// -----------------------------------------------------------------------
// <copyright file="SessionTemplateViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using QDMS.Server.Validation;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using MahApps.Metro.Controls.Dialogs;

namespace QDMSServer.ViewModels
{
    public class SessionTemplateViewModel : ValidatingViewModelBase<SessionTemplate>
    {
        private SessionViewModel _selectedSession;

        public SessionTemplateViewModel(SessionTemplate model, IDataClient client, object parentVm, IDialogCoordinator dialogCoordinator)
            : base(model, new SessionTemplateValidator())
        {
            var canExecuteSave = this.WhenAnyValue(x => x.HasErrors, x => !x);
            Save = ReactiveCommand.CreateFromTask(async _ =>
            {
                var result = await client.UpdateSessionTemplate(this.Model).ConfigureAwait(true);
                await result.DisplayErrors(parentVm, dialogCoordinator).ConfigureAwait(true);
            },
            canExecuteSave);

            var canRemove = this.WhenAnyValue(x => x.SelectedSession).Select(x => x != null);
            RemoveSession = ReactiveCommand.Create<SessionViewModel>(viewModel =>
            {
                Sessions.Remove(viewModel);
                Model.Sessions.Remove((TemplateSession)viewModel.Model);
            }, 
            canRemove);

            AddSession = ReactiveCommand.Create(() =>
            {
                var toAdd = new TemplateSession { IsSessionEnd = true };

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
            if (model.Sessions != null)
            {
                foreach (var session in model.Sessions)
                {
                    Sessions.Add(new SessionViewModel(session));
                }
            }
        }

        public ReactiveCommand<Unit, Unit> AddSession { get; set; }

        public ReactiveCommand<SessionViewModel, Unit> RemoveSession { get; set; }

        public ReactiveCommand<Unit, Unit> Save { get; }

        public string Name
        {
            get => Model.Name;
            set { Model.Name = value; this.RaisePropertyChanged(); }
        }

        public ReactiveList<SessionViewModel> Sessions { get; set; } = new ReactiveList<SessionViewModel>();

        public SessionViewModel SelectedSession
        {
            get => _selectedSession;
            set => this.RaiseAndSetIfChanged(ref _selectedSession, value);
        }
    }
}