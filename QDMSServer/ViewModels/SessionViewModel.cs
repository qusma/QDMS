// -----------------------------------------------------------------------
// <copyright file="SessionViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using QDMS.Server.Validation;
using ReactiveUI;
using System;

namespace QDMSServer.ViewModels
{
    public class SessionViewModel : ValidatingViewModelBase<ISession>
    {
        public SessionViewModel(ISession model) : base(model, new SessionValidator())
        {
        }

        public DayOfTheWeek OpeningDay
        {
            get => Model.OpeningDay;
            set
            {
                if (value == Model.OpeningDay) return;
                Model.OpeningDay = value;
                this.RaisePropertyChanged();
            }
        }

        public DayOfTheWeek ClosingDay
        {
            get => Model.ClosingDay;
            set
            {
                if (value == Model.ClosingDay) return;
                Model.ClosingDay = value;
                this.RaisePropertyChanged();
            }
        }


        public int ID
        {
            get => Model.ID;
            set
            {
                if (value == Model.ID) return;
                Model.ID = value;
                this.RaisePropertyChanged();
            }
        }

        public TimeSpan OpeningTime
        {
            get => Model.OpeningTime;
            set
            {
                if (value == Model.OpeningTime) return;
                Model.OpeningTime = value;
                this.RaisePropertyChanged();
            }
        }
        public TimeSpan ClosingTime
        {
            get => Model.ClosingTime;
            set
            {
                if (value == Model.ClosingTime) return;
                Model.ClosingTime = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsSessionEnd
        {
            get => Model.IsSessionEnd;
            set
            {
                if (value == Model.IsSessionEnd) return;
                Model.IsSessionEnd = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
