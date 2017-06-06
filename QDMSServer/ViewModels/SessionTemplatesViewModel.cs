// -----------------------------------------------------------------------
// <copyright file="SessionTemplatesViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using ReactiveUI;

namespace QDMSServer.ViewModels
{
    public class SessionTemplatesViewModel : ReactiveObject
    {
        private SessionTemplateViewModel _selectedTemplate;
        public ReactiveList<SessionTemplateViewModel> Templates { get; } = new ReactiveList<SessionTemplateViewModel>();

        public SessionTemplateViewModel SelectedTemplate
        {
            get => _selectedTemplate;
            set => this.RaiseAndSetIfChanged(ref _selectedTemplate, value);
        }

        public SessionTemplatesViewModel(IDataClient client, IDialogCoordinator dialogCoordinator)
        {
            Load = ReactiveCommand.CreateFromTask(async _ =>
            {
                var result = await client.GetSessionTemplates().ConfigureAwait(true);
                if (await result.DisplayErrors(this, dialogCoordinator).ConfigureAwait(true))
                {
                    return;
                }

                foreach (var template in result.Result)
                {
                    Templates.Add(new SessionTemplateViewModel(template, client, this, dialogCoordinator));
                }
            });

            var canDelete = this.WhenAnyValue(x => x.SelectedTemplate).Select(x => x != null);
            Delete = ReactiveCommand.CreateFromTask(async _ =>
            {
                var confirm = await dialogCoordinator.ShowMessageAsync(this, "Delete", $"Are you sure you want to delete {SelectedTemplate.Name}?", MessageDialogStyle.AffirmativeAndNegative).ConfigureAwait(true);
                if (confirm == MessageDialogResult.Negative) return;

                var result = await client.DeleteSessionTemplate(SelectedTemplate.Model).ConfigureAwait(true);
                if (await result.DisplayErrors(this, dialogCoordinator).ConfigureAwait(true))
                {
                    return;
                }

                Templates.Remove(SelectedTemplate);
            },
            canDelete);

            Add = ReactiveCommand.CreateFromTask(async _ =>
            {
                var newTemplate = new SessionTemplate { Name = "New Template", Sessions = new List<TemplateSession>() };
                var addResult = await client.AddSessionTemplate(newTemplate).ConfigureAwait(true);
                if (await addResult.DisplayErrors(this, dialogCoordinator).ConfigureAwait(true))
                {
                    return;
                }

                var vm = new SessionTemplateViewModel(addResult.Result, client, this, dialogCoordinator);
                Templates.Add(vm);
                SelectedTemplate = vm;
            });
        }

        public ReactiveCommand<Unit, Unit> Add { get; set; }

        public ReactiveCommand<Unit, Unit> Delete { get; set; }

        public ReactiveCommand<Unit, Unit> Load { get; set; }
    }
}
