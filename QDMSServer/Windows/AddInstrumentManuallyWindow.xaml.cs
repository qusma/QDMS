// -----------------------------------------------------------------------
// <copyright file="AddInstrumentManuallyWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EntityData;
using QDMS;

namespace QDMSServer
{
    
    public partial class AddInstrumentManuallyWindow
    {
        public Instrument TheInstrument { get; set; }
        public ObservableCollection<CheckBoxTag> Tags { get; set; }
        private readonly Instrument _originalInstrument;
        public ObservableCollection<Exchange> Exchanges { get; set; }
        public ObservableCollection<KeyValuePair<int, string>> ContractMonths { get; set; }
        public bool InstrumentAdded = false;
        private readonly bool _addingNew;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instrument">If we're updating or cloning an instrument, pass it here.</param>
        /// <param name="addingNew">True if adding a new instrument. False if we're updating an instrument.</param>
        public AddInstrumentManuallyWindow(Instrument instrument = null, bool addingNew = true, bool addingContFut = false)
        {
            InitializeComponent();

            //If it's a continuous future, make the continuous future tab visible
            if ((instrument != null && instrument.IsContinuousFuture) ||
                addingContFut)
            {
                ContFutTabItem.Visibility = Visibility.Visible;
                TypeComboBox.IsEnabled = false;
            }
            else
            {
                ContFutTabItem.Visibility = Visibility.Hidden;
            }

            DataContext = this;
            _addingNew = addingNew;

            var context = new MyDBContext();



            if (instrument != null)
            {
                context.Instruments.Attach(instrument);
                context.Entry(instrument).Reload();
                if(instrument.Exchange != null)
                    context.Entry(instrument.Exchange).Reload();

                TheInstrument = (Instrument)instrument.Clone();
                if (TheInstrument.Tags == null) TheInstrument.Tags = new List<Tag>();
                if (TheInstrument.Sessions == null) TheInstrument.Sessions = new List<InstrumentSession>();
                TheInstrument.Sessions = TheInstrument.Sessions.OrderBy(x => x.OpeningDay).ThenBy(x => x.OpeningTime).ToList();

                if (TheInstrument.IsContinuousFuture)
                {
                    TheInstrument.ContinuousFuture = (ContinuousFuture) instrument.ContinuousFuture.Clone();
                }
            }
            else
            {
                TheInstrument = new Instrument 
                {
                    Tags = new List<Tag>(), 
                    Sessions = new List<InstrumentSession>()
                };

                //need to do some extra stuff if it's a continuous future
                if (addingContFut)
                {
                    TheInstrument.ContinuousFuture = new ContinuousFuture();
                    TheInstrument.Type = InstrumentType.Future;
                    TheInstrument.IsContinuousFuture = true;
                }

                CustomRadioBtn.IsChecked = true;
            }

            Tags = new ObservableCollection<CheckBoxTag>();
            foreach (Tag t in context.Tags)
            {
                Tags.Add(new CheckBoxTag(t, TheInstrument.Tags.Contains(t)));
            }

            if (addingNew)
            {
                Title = "Add New Instrument";
                AddBtn.Content = "Add";
            }
            else
            {
                Title = "Modify Instrument";
                AddBtn.Content = "Modify";
                _originalInstrument = instrument;
            }

            

            Exchanges = new ObservableCollection<Exchange>();

            var exchangeList = context.Exchanges.AsEnumerable().OrderBy(x => x.Name);
            foreach (Exchange e in exchangeList)
            {
                Exchanges.Add(e);
            }


            //fill template box
            var templates = context.SessionTemplates.Include("Sessions").ToList();
            foreach (SessionTemplate t in templates)
            {
                TemplateComboBox.Items.Add(t);
            }
            if (TheInstrument.SessionsSource == SessionsSource.Template)
            {
                TemplateComboBox.SelectedItem = templates.First(x => x.ID == TheInstrument.SessionTemplateID);
            }

            //set the right radio button...
            CustomRadioBtn.IsChecked = TheInstrument.SessionsSource == SessionsSource.Custom;
            TemplateRadioBtn.IsChecked = TheInstrument.SessionsSource == SessionsSource.Template;
            ExchangeRadioBtn.IsChecked = TheInstrument.SessionsSource == SessionsSource.Exchange;

            //populate instrument type combobox with enum values
            var instrumentTypeValues = MyUtils.GetEnumValues<InstrumentType>();
            foreach (InstrumentType t in instrumentTypeValues)
            {
                TypeComboBox.Items.Add(t);
            }

            //populate option type combobox with enum values
            var optionTypeValues = MyUtils.GetEnumValues<OptionType>();
            foreach (OptionType t in optionTypeValues)
            {
                OptionTypeComboBox.Items.Add(t);
            }

            var dataSources = context.Datasources.AsEnumerable();
            foreach (Datasource d in dataSources)
            {
                DatasourceComboBox.Items.Add(d);
            }

            //sort the sessions so they're ordered properly...
            SessionsGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("OpeningDay", System.ComponentModel.ListSortDirection.Ascending));

            //fill the RolloverRuleType combobox
            var rolloverTypes = MyUtils.GetEnumValues<ContinuousFuturesRolloverType>();
            foreach (ContinuousFuturesRolloverType t in rolloverTypes)
            {
                if(t != ContinuousFuturesRolloverType.Time)
                    RolloverRuleType.Items.Add(t);
            }

            //fill the RootSymbolComboBox
            foreach (UnderlyingSymbol s in context.UnderlyingSymbols)
            {
                RootSymbolComboBox.Items.Add(s);
            }

            ContractMonths = new ObservableCollection<KeyValuePair<int, string>>();
            //fill the continuous futures contrat month combobox
            for (int i = 1; i < 10; i++)
            {
                ContractMonths.Add(new KeyValuePair<int, string>(i, MyUtils.Ordinal(i) + " Contract"));
            }

            //time or rule-based rollover, set the radio button check
            if (TheInstrument.ContinuousFuture != null)
            {
                if (TheInstrument.ContinuousFuture.RolloverType == ContinuousFuturesRolloverType.Time)
                {
                    RolloverTime.IsChecked = true;
                }
                else
                {
                    RolloverRule.IsChecked = true;
                }
            }
            
            context.Dispose();
        }


        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new MyDBContext())
            {
                if (_addingNew &&
                    context.Instruments.Any(
                        x => x.DatasourceID == TheInstrument.DatasourceID &&
                             x.ExchangeID == TheInstrument.ExchangeID &&
                             x.Symbol == TheInstrument.Symbol &&
                             x.Expiration == TheInstrument.Expiration)
                    )
                {
                    //there's already an instrument with this key
                    MessageBox.Show("Instrument already exists. Change datasource, exchange, or symbol.");
                    return;
                }

                //check that if the user picked a template-based session set, he actually selected one of the templates
                if (TheInstrument.SessionsSource == SessionsSource.Template && TheInstrument.SessionTemplateID == -1)
                {
                    MessageBox.Show("You must pick a session template.");
                    return;
                }

                if (TheInstrument.IsContinuousFuture && TheInstrument.Type != InstrumentType.Future)
                {
                    MessageBox.Show("Continuous futures type must be Future.");
                    return;
                }

                if (TheInstrument.Datasource == null)
                {
                    MessageBox.Show("You must select a data source.");
                    return;
                }

                if (TheInstrument.Multiplier == null)
                {
                    MessageBox.Show("Must have a multiplier value.");
                    return;
                }

                TheInstrument.Tags.Clear();
            
                foreach (Tag t in Tags.Where(x=> x.IsChecked).Select(x => x.Item))
                {
                    context.Tags.Attach(t);
                    TheInstrument.Tags.Add(t);
                }

                ContinuousFuture tmpCF = null;

                if (_addingNew)
                {
                    if (TheInstrument.Exchange != null) context.Exchanges.Attach(TheInstrument.Exchange);
                    if (TheInstrument.PrimaryExchange != null) context.Exchanges.Attach(TheInstrument.PrimaryExchange);
                    context.Datasources.Attach(TheInstrument.Datasource);

                    if (TheInstrument.IsContinuousFuture)
                    {
                        tmpCF = TheInstrument.ContinuousFuture; //EF can't handle circular references, so we hack around it
                        TheInstrument.ContinuousFuture = null;
                        TheInstrument.ContinuousFutureID = null;
                    }
                    context.Instruments.Add(TheInstrument);
                }
                else //simply manipulating an existing instrument
                {
                    if (TheInstrument.Exchange != null)
                        context.Exchanges.Attach(TheInstrument.Exchange);
                    if(TheInstrument.PrimaryExchange != null)
                        context.Exchanges.Attach(TheInstrument.PrimaryExchange);

                    context.Datasources.Attach(TheInstrument.Datasource);

                    context.Instruments.Attach(_originalInstrument);
                    context.Entry(_originalInstrument).CurrentValues.SetValues(TheInstrument);

                    if (TheInstrument.IsContinuousFuture)
                    {
                        //TheInstrument.ContinuousFuture.InstrumentID
                        context.ContinuousFutures.Attach(TheInstrument.ContinuousFuture);
                    }

                    _originalInstrument.Tags.Clear();
                    foreach (Tag t in TheInstrument.Tags)
                    {
                        _originalInstrument.Tags.Add(t);
                    }

                    var sessions = _originalInstrument.Sessions.ToList();
                    foreach (InstrumentSession i in sessions)
                    {
                        context.Entry(i).State = System.Data.Entity.EntityState.Deleted;
                    }

                    _originalInstrument.Sessions.Clear();

                    foreach (InstrumentSession s in TheInstrument.Sessions)
                    {
                        _originalInstrument.Sessions.Add(s);
                    }
                }

                context.Database.Connection.Open();
                context.SaveChanges();

                if (tmpCF != null)
                {
                    TheInstrument.ContinuousFuture = tmpCF;
                    TheInstrument.ContinuousFuture.Instrument = TheInstrument;
                    TheInstrument.ContinuousFuture.InstrumentID = TheInstrument.ID.Value;
                    context.SaveChanges();
                }
            }
            InstrumentAdded = true;
            Hide();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OptionTypeNullCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkEdit = sender as CheckBox;
            if (checkEdit != null && (!checkEdit.IsChecked.HasValue || checkEdit.IsChecked.Value == false))
            {
                TheInstrument.OptionType = (OptionType?)OptionTypeComboBox.SelectedItem;
            }
            else
            {
                TheInstrument.OptionType = null;
            }
        }

        private void ExchangeRadioBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (TheInstrument.Exchange == null) return;
            if (TheInstrument.SessionsSource == SessionsSource.Exchange) return; //we don't want to re-load them if it's already set

            TheInstrument.Sessions.Clear();
            TheInstrument.SessionsSource = SessionsSource.Exchange;

            //if we're changing exchanges, the sessions will not have been loaded, so we need to grab them
            using (var context = new MyDBContext())
            {
                context.Exchanges.Attach(TheInstrument.Exchange);
                var sessions = context.Exchanges.Include("Sessions").Where(x => x.ID == TheInstrument.Exchange.ID).ToList();
            }

            foreach (ExchangeSession s in TheInstrument.Exchange.Sessions)
            {
                TheInstrument.Sessions.Add(MyUtils.SessionConverter(s));
            }
            SessionsGrid.ItemsSource = null;
            SessionsGrid.ItemsSource = TheInstrument.Sessions;
        }

        private void CustomRadioBtn_Checked(object sender, RoutedEventArgs e)
        {
            TheInstrument.SessionsSource = SessionsSource.Custom;
        }

        private void TemplateRadioBtn_Checked(object sender, RoutedEventArgs e)
        {
            TheInstrument.SessionsSource = SessionsSource.Template;
            FillSessionsFromTemplate();
        }

        private void TemplateComboBox_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            FillSessionsFromTemplate();
        }

        private void FillSessionsFromTemplate()
        {
            TheInstrument.Sessions.Clear();
            
            var template = (SessionTemplate)TemplateComboBox.SelectedItem;
            if (template == null)
            {
                TheInstrument.SessionTemplateID = -1; //we can check for this later and deny the new instrument if its sessions are not set properly
                return;
            }

            TheInstrument.SessionTemplateID = template.ID;
            foreach (TemplateSession s in template.Sessions.OrderBy(x => x.OpeningDay))
            {
                TheInstrument.Sessions.Add(MyUtils.SessionConverter(s));
            }
            SessionsGrid.ItemsSource = null;
            SessionsGrid.ItemsSource = TheInstrument.Sessions;
        }

        private void AddSessionItemBtn_Click(object sender, RoutedEventArgs e)
        {
            var toAdd = new InstrumentSession {IsSessionEnd = true};

            if (TheInstrument.Sessions.Count == 0)
            {
                toAdd.OpeningDay = DayOfTheWeek.Monday;
                toAdd.ClosingDay = DayOfTheWeek.Monday;
            }
            else
            {
                DayOfTheWeek maxDay = (DayOfTheWeek)Math.Min(6, TheInstrument.Sessions.Max(x => (int)x.OpeningDay) + 1);
                toAdd.OpeningDay = maxDay;
                toAdd.ClosingDay = maxDay;
            }
            TheInstrument.Sessions.Add(toAdd);
            SessionsGrid.ItemsSource = null;
            SessionsGrid.ItemsSource = TheInstrument.Sessions;
        }

        private void DeleteSessionItemBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedSession = (InstrumentSession)SessionsGrid.SelectedItem;
            TheInstrument.Sessions.Remove(selectedSession);
            SessionsGrid.ItemsSource = null;
            SessionsGrid.ItemsSource = TheInstrument.Sessions;
        }

        private void ExchangeComboBox_OnSelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (TheInstrument.SessionsSource == SessionsSource.Exchange)
            {
                using (var context = new MyDBContext())
                {
                    TheInstrument.Sessions.Clear();
                    var exchange = context.Exchanges.First(x => x.Name == TheInstrument.Exchange.Name);
                    foreach (ExchangeSession s in exchange.Sessions)
                    {
                        TheInstrument.Sessions.Add(MyUtils.SessionConverter(s));
                    }
                    SessionsGrid.ItemsSource = null;
                    SessionsGrid.ItemsSource = TheInstrument.Sessions;
                }
            }
        }

        private void RolloverRule_Checked(object sender, RoutedEventArgs e)
        {
            TheInstrument.ContinuousFuture.RolloverType = (ContinuousFuturesRolloverType) RolloverRuleType.SelectedItem;
        }

        private void RolloverTime_Checked(object sender, RoutedEventArgs e)
        {
            TheInstrument.ContinuousFuture.RolloverType = ContinuousFuturesRolloverType.Time;
        }
    }
}
