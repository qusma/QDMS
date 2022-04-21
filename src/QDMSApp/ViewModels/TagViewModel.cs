// -----------------------------------------------------------------------
// <copyright file="TagViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using QDMS.Server.Validation;
using ReactiveUI;

namespace QDMSApp.ViewModels
{
    public class TagViewModel : ValidatingViewModelBase<Tag>
    {
        private bool _confirmDelete;

        /// <summary>
        /// Used to require two clicks on the delete button before deleting a tag
        /// </summary>
        public bool ConfirmDelete
        {
            get { return _confirmDelete; }
            set { this.RaiseAndSetIfChanged(ref _confirmDelete, value); }
        }

        public string Name
        {
            get { return Model.Name; }
            set
            {
                if (value == Model.Name) return;
                Model.Name = value;
                this.RaisePropertyChanged();
            }
        }

        public TagViewModel(Tag tag) : base(tag, new TagValidator())
        {
        }
    }
}