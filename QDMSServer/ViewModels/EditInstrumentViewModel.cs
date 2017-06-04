// -----------------------------------------------------------------------
// <copyright file="EditInstrumentViewModel.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMS.Server.Validation;
using QDMSClient;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace QDMSServer.ViewModels
{
    public class EditInstrumentViewModel : ValidatingViewModelBase<Instrument>
    {
        private readonly IClosableView _view;
        public ReactiveCommand<Unit, Unit> Load { get; }
        public IDialogCoordinator DialogCoordinator { get; }
        public ReactiveList<CheckBoxTag> AllTags { get; } = new ReactiveList<CheckBoxTag>();
        public ReactiveList<Exchange> Exchanges { get; } = new ReactiveList<Exchange>();
        public ReactiveList<SessionTemplate> SessionTemplates { get; } = new ReactiveList<SessionTemplate>();
        public ReactiveList<UnderlyingSymbol> UnderlyingSymbols { get; } = new ReactiveList<UnderlyingSymbol>();
        public ReactiveList<Datasource> Datasources { get; } = new ReactiveList<Datasource>();
        public ReactiveCommand<SessionViewModel, Unit> RemoveSession { get; set; }
        public ReactiveCommand<Unit, Unit> AddNewSession { get; set; }
        public ReactiveList<SessionViewModel> Sessions { get; } = new ReactiveList<SessionViewModel>();
        public ReactiveCommand<Unit, ApiResponse<Instrument>> Save { get; set; }
        public ObservableCollection<KeyValuePair<int, string>> ContractMonths { get; set; }

        /// <summary>
        /// If an instrument is successfully added/updated, it will be found here
        /// </summary>
        public Instrument AddedInstrument { get; set; }

        [Obsolete("For design-time use only")]
        public EditInstrumentViewModel() : base(null, null) { }

        public EditInstrumentViewModel(Instrument model, IDataClient client, IClosableView view, IDialogCoordinator dialogCoordinator) : base(model, new InstrumentValidator())
        {
            _view = view;
            DialogCoordinator = dialogCoordinator;
            if (model.Sessions == null) model.Sessions = new List<InstrumentSession>();
            if (model.Tags == null) model.Tags = new List<Tag>();

            foreach (var session in model.Sessions)
            {
                Sessions.Add(new SessionViewModel(session));
            }

            ContractMonths = new ObservableCollection<KeyValuePair<int, string>>();
            //fill the continuous futures contrat month combobox
            for (int i = 1; i < 10; i++)
            {
                ContractMonths.Add(new KeyValuePair<int, string>(i, MyUtils.Ordinal(i) + " Contract"));
            }

            Load = ReactiveCommand.CreateFromTask(async _ =>
            {
                var tags = client.GetTags();
                var sessionTemplates = client.GetSessionTemplates();
                var exchanges = client.GetExchanges();
                var underlyingSymbols = client.GetUnderlyingSymbols();
                var datasources = client.GetDatasources();

                await Task.WhenAll(tags, sessionTemplates, exchanges, underlyingSymbols, datasources).ConfigureAwait(true);

                var responses = new ApiResponse[] { tags.Result, sessionTemplates.Result, exchanges.Result, underlyingSymbols.Result, datasources.Result };
                if (await responses.DisplayErrors(this, DialogCoordinator).ConfigureAwait(true)) return;

                foreach (var tag in tags.Result.Result.Select(x => new CheckBoxTag(x, model.Tags.Contains(x))))
                {
                    AllTags.Add(tag);
                    tag.PropertyChanged += Tag_PropertyChanged;
                }

                Exchanges.AddRange(exchanges.Result.Result);

                foreach (var template in sessionTemplates.Result.Result)
                {
                    SessionTemplates.Add(template);
                }
                foreach (var us in underlyingSymbols.Result.Result)
                {
                    UnderlyingSymbols.Add(us);
                }
                foreach (var ds in datasources.Result.Result)
                {
                    Datasources.Add(ds);
                }
            });

            //Sessions
            AddNewSession = ReactiveCommand.Create(() => AddSession());
            RemoveSession = ReactiveCommand.Create<SessionViewModel>(ExecRemoveSession);

            //Save
            var saveCanExecute = this
                .WhenAnyValue(x => x.HasErrors)
                .Select(x => !x);
            Save = ReactiveCommand.CreateFromTask(async _ =>
            {
                if (model.ID == null || model.ID <= 0)
                {
                    //adding a new instrument
                    return await client.AddInstrument(model).ConfigureAwait(true);
                }
                else
                {
                    //updating an existing one
                    return await client.UpdateInstrument(model).ConfigureAwait(true);
                }
            }
            , saveCanExecute);
            Save.Subscribe(async result =>
            {
                var errors = await result.DisplayErrors(this, DialogCoordinator).ConfigureAwait(true);
                if (!errors)
                {
                    AddedInstrument = result.Result;
                    _view.Close();
                }
            });

            this.Validate();
        }

        private void Tag_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //fires when a tag is checked
            var checkbox = sender as CheckBoxTag;
            if (checkbox == null) return;

            if(checkbox.IsChecked && !Model.Tags.Contains(checkbox.Item))
            {
                Model.Tags.Add(checkbox.Item);
            }
            
            if(!checkbox.IsChecked)
            {
                Model.Tags.Remove(checkbox.Item);
            }
        }

        private void ClearSessions()
        {
            Sessions.Clear();
            Model.Sessions.Clear();
        }

        private void RefreshSessions()
        {
            if (Model.SessionsSource == SessionsSource.Exchange)
            {
                ClearSessions();

                if (Exchange != null)
                {
                    foreach (ExchangeSession s in Exchange?.Sessions)
                    {
                        AddSession(s.ToInstrumentSession());
                    }
                }
            }
            else if (Model.SessionsSource == SessionsSource.Template)
            {
                ClearSessions();

                if (SessionTemplateID != null)
                {
                    var template = SessionTemplates.FirstOrDefault(x => x.ID == SessionTemplateID);
                    if (template != null)
                    {
                        foreach (TemplateSession s in template.Sessions)
                        {
                            AddSession(s.ToInstrumentSession());
                        }
                    }
                }
            }
            else
            {
                Model.SessionTemplateID = -1;
                ClearSessions();
            }

            this.RaisePropertyChanged(nameof(SessionTemplateID)); //this is here for validation in the GUI to work properly
        }

        private void AddSession(InstrumentSession session = null)
        {
            if (session != null)
            {
                Sessions.Add(new SessionViewModel(session));
                Model.Sessions.Add(session);
                return;
            }

            var toAdd = new InstrumentSession { IsSessionEnd = true };

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
        }

        private void ExecRemoveSession(SessionViewModel session)
        {
            Sessions.Remove(session);
            Model.Sessions.Remove((InstrumentSession)session.Model);
        }

        public int? ID => Model.ID;
        public string TagsAsString => Model.TagsAsString;

        public string Symbol
        {
            get => Model.Symbol;
            set
            {
                if (value == Model.Symbol) return;
                Model.Symbol = value;
                this.RaisePropertyChanged();
            }
        }

        public string UnderlyingSymbol
        {
            get => Model.UnderlyingSymbol;
            set
            {
                if (value == Model.UnderlyingSymbol) return;
                Model.UnderlyingSymbol = value;
                this.RaisePropertyChanged();
            }
        }

        public string DatasourceSymbol
        {
            get => Model.DatasourceSymbol;
            set
            {
                if (value == Model.DatasourceSymbol) return;
                Model.DatasourceSymbol = value;
                this.RaisePropertyChanged();
            }
        }

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

        public Exchange PrimaryExchange
        {
            get => Model.PrimaryExchange;
            set
            {
                Model.PrimaryExchange = value;
                Model.PrimaryExchangeID = value?.ID;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Model.PrimaryExchangeID));
            }
        }

        public Exchange Exchange
        {
            get => Model.Exchange;
            set
            {
                Model.Exchange = value;
                Model.ExchangeID = value?.ID;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Model.ExchangeID));
            }
        }

        public Datasource Datasource
        {
            get => Model.Datasource;
            set
            {
                Model.Datasource = value;
                Model.DatasourceID = value?.ID;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Model.DatasourceID));
            }
        }

        public ContinuousFuture ContinuousFuture
        {
            get => Model.ContinuousFuture;
            set
            {
                Model.ContinuousFuture = value;
                Model.ContinuousFutureID = value?.ID;
                this.RaisePropertyChanged();
                this.RaisePropertyChanged(nameof(Model.ContinuousFutureID));
            }
        }

        public InstrumentType Type
        {
            get => Model.Type;
            set
            {
                if (value == Model.Type) return;
                Model.Type = value;
                this.RaisePropertyChanged();
            }
        }

        public int? Multiplier
        {
            get => Model.Multiplier;
            set
            {
                if (value == Model.Multiplier) return;
                Model.Multiplier = value;
                this.RaisePropertyChanged();
            }
        }

        public DateTime? Expiration
        {
            get => Model.Expiration;
            set
            {
                if (value == Model.Expiration) return;
                Model.Expiration = value;
                this.RaisePropertyChanged();
            }
        }

        public OptionType? OptionType
        {
            get => Model.OptionType;
            set
            {
                if (value == Model.OptionType) return;
                Model.OptionType = value;
                this.RaisePropertyChanged();
            }
        }

        public bool OptionTypeNull
        {
            get => Model.OptionType == null;
            set
            {
                OptionType = value ? (OptionType?)null : QDMS.OptionType.Call;
                this.RaisePropertyChanged();
            }
        }

        public bool PrimaryExchangeNull
        {
            get => Model.PrimaryExchange == null;
            set
            {
                PrimaryExchange = value ? null : Exchanges.First();
                this.RaisePropertyChanged();
            }
        }

        public decimal? Strike
        {
            get => Model.Strike;
            set
            {
                if (value == Model.Strike) return;
                Model.Strike = value;
                this.RaisePropertyChanged();
            }
        }

        public string Currency
        {
            get => Model.Currency;
            set
            {
                if (value == Model.Currency) return;
                Model.Currency = value;
                this.RaisePropertyChanged();
            }
        }

        public decimal? MinTick
        {
            get => Model.MinTick;
            set
            {
                if (value == Model.MinTick) return;
                Model.MinTick = value;
                this.RaisePropertyChanged();
            }
        }

        public string Industry
        {
            get => Model.Industry;
            set
            {
                if (value == Model.Industry) return;
                Model.Industry = value;
                this.RaisePropertyChanged();
            }
        }

        public string Category
        {
            get => Model.Category;
            set
            {
                if (value == Model.Category) return;
                Model.Category = value;
                this.RaisePropertyChanged();
            }
        }

        public string Subcategory
        {
            get => Model.Subcategory;
            set
            {
                if (value == Model.Subcategory) return;
                Model.Subcategory = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsContinuousFuture
        {
            get => Model.IsContinuousFuture;
            set
            {
                if (value == Model.IsContinuousFuture) return;
                Model.IsContinuousFuture = value;
                this.RaisePropertyChanged();
            }
        }

        public string ValidExchanges
        {
            get => Model.ValidExchanges;
            set
            {
                if (value == Model.ValidExchanges) return;
                Model.ValidExchanges = value;
                this.RaisePropertyChanged();
            }
        }

        public SessionsSource SessionsSource
        {
            get => Model.SessionsSource;
            set
            {
                if (value == Model.SessionsSource) return;
                Model.SessionsSource = value;
                this.RaisePropertyChanged();
                RefreshSessions(); //here because doing it as a command was buggy for some reason
            }
        }

        public int? SessionTemplateID
        {
            get => Model.SessionTemplateID;
            set
            {
                if (value == Model.SessionTemplateID) return;
                Model.SessionTemplateID = value;
                this.RaisePropertyChanged();
            }
        }

        public string TradingClass
        {
            get => Model.TradingClass;
            set
            {
                if (value == Model.TradingClass) return;
                Model.TradingClass = value;
                this.RaisePropertyChanged();
            }
        }
    }
}