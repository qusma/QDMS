// -----------------------------------------------------------------------
// <copyright file="TagsViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSClient;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace QDMSServer.ViewModels
{
    public class TagsViewModel : ReactiveObject
    {
        private readonly QDMSClient.QDMSClient _client;
        private readonly IDialogCoordinator _dialogCoordinator;
        private ObservableCollection<TagViewModel> _tags;
        private TagViewModel _selectedTag;

        public TagViewModel NewTag { get; set; } = new TagViewModel(new Tag());

        public ObservableCollection<TagViewModel> Tags
        {
            get { return _tags; }
            set { this.RaiseAndSetIfChanged(ref _tags, value); }
        }

        public TagViewModel SelectedTag
        {
            get { return _selectedTag; }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedTag, value);

                //update the SelectedTagHasErrors subscription
                _selectedTagHasErrorsSubscription?.Dispose();
                if (_selectedTag == null) return;
                _selectedTagHasErrorsSubscription = _selectedTag
                    .WhenAnyValue(x => x.HasErrors)
                    .Subscribe(_ => this.RaisePropertyChanged(nameof(SelectedTagHasErrors)));
            }
        }

        IDisposable _selectedTagHasErrorsSubscription;

        /// <summary>
        /// Conveniently passes through HasErrors for the selected tag
        /// </summary>
        public bool SelectedTagHasErrors => SelectedTag?.HasErrors ?? false;

        public TagsViewModel(QDMSClient.QDMSClient client, IDialogCoordinator dialogCoordinator)
        {
            _client = client;
            _dialogCoordinator = dialogCoordinator;
            Tags = new ObservableCollection<TagViewModel>();

            //Load all tags from db
            LoadTags = ReactiveCommand.CreateFromTask(async _ => await _client.GetTags().ConfigureAwait(true));
            LoadTags.Subscribe(async result =>
            {
                if (!result.WasSuccessful)
                {
                    await _dialogCoordinator.ShowMessageAsync(this, "Error", string.Join("\n", result.Errors)).ConfigureAwait(true);
                    return;
                }

                foreach (var tag in result.Result)
                {
                    Tags.Add(new TagViewModel(tag));
                }
            });

            //Add new tag
            var addCanExecute = NewTag
                .WhenAnyValue(x => x.Name)
                .Select(newName => !string.IsNullOrEmpty(newName));
            Add = ReactiveCommand.CreateFromTask(async _ =>
            {
                var tag = await client.AddTag(NewTag.Model).ConfigureAwait(true);
                NewTag.Name = "";
                return tag;
            },
            addCanExecute);

            Add.Subscribe(async result =>
            {
                if (!result.WasSuccessful)
                {
                    await _dialogCoordinator.ShowMessageAsync(this, "Error", string.Join("\n", result.Errors)).ConfigureAwait(true);
                    return;
                }
                Tags.Add(new TagViewModel(result.Result));
            });

            //When changing the selected tag, reset the delete confirmation
            this.WhenAnyValue(x => x.SelectedTag)
                .Buffer(1, 1)
                .Subscribe(x => { var tagVm = x.FirstOrDefault(); if (tagVm != null) tagVm.ConfirmDelete = false; });

            //Delete Tag
            Delete = ReactiveCommand.CreateFromTask(async _ =>
            {
                if (SelectedTag.ConfirmDelete != true)
                {
                    SelectedTag.ConfirmDelete = true;
                    return null;
                }

                return await client.DeleteTag(SelectedTag?.Model).ConfigureAwait(true);
            },
            this.WhenAnyValue(x => x.SelectedTag).Select(x => x != null));

            Delete.Subscribe(async result =>
            {
                if (result == null) return;
                if (await result.DisplayErrors(this, _dialogCoordinator)) return;

                Tags.Remove(Tags.FirstOrDefault(x => x.Model.ID == result.Result.ID));
            });

            //Update Tag
            var saveCanExecute = this
                .WhenAnyValue(x => x.SelectedTag, x => x.SelectedTagHasErrors, (tag, hasError) => new { tag, hasError })
                .Select(x => x.tag != null && x.hasError == false);

            Save = ReactiveCommand.CreateFromTask(
                async _ => await client.UpdateTag(SelectedTag?.Model).ConfigureAwait(true),
                saveCanExecute);
            Save.Subscribe(async result => await result.DisplayErrors(this, _dialogCoordinator));
        }

        public ReactiveCommand<Unit, ApiResponse<List<Tag>>> LoadTags { get; set; }

        public ReactiveCommand<Unit, ApiResponse<Tag>> Add { get; set; }
        public ReactiveCommand<Unit, ApiResponse<Tag>> Save { get; set; }
        public ReactiveCommand<Unit, ApiResponse<Tag>> Delete { get; set; }
    }
}