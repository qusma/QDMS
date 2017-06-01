// -----------------------------------------------------------------------
// <copyright file="IJobViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using QDMSClient;
using ReactiveUI;
using System.ComponentModel;
using System.Reactive;

namespace QDMSServer
{
    public interface IJobViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        string Name { get; set; }

        IJobSettings Job { get; }

        /// <summary>
        /// When changing the name, we need to keep track of the previous one as well to unschedule the previous job
        /// This is necessary because you can't "update" jobs, only delete and add from scratch
        /// </summary>
        string PreChangeName { get; set; }

        ReactiveCommand<Unit, Unit> Save { get; }

        ReactiveCommand<Unit, ApiResponse> Delete { get; }
    }
}