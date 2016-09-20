// -----------------------------------------------------------------------
// <copyright file="IJobViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.ComponentModel;
using Quartz;
using ReactiveUI;

namespace QDMSServer
{
    public interface IJobViewModel : INotifyPropertyChanged
    {
        string ValidationError { get; set; }
        ReactiveCommand<object> Save { get; }
        void DeleteJob();
        string Name { get; set; }
        JobKey JobKey { get; }
    }
}
