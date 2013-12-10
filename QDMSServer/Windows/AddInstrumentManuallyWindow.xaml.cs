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
        public bool InstrumentAdded = false;
        private readonly bool _addingNew;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instrument">If we're updating or cloning an instrument, pass it here.</param>
        /// <param name="addingNew">True if adding a new instrument. False if we're updating an instrument.</param>
        public AddInstrumentManuallyWindow(Instrument instrument = null, bool addingNew = true)
        {
            InitializeComponent();

            DataContext = this;
            _addingNew = addingNew;

            var context = new MyDBContext();



            if (instrument != null)
            {
                context.Instruments.Attach(instrument);
                context.Entry(instrument).Reload();
                context.Entry(instrument.Exchange).Reload();
                TheInstrument = (Instrument)instrument.Clone();
                if (TheInstrument.Tags == null) TheInstrument.Tags = new List<Tag>();
                if (TheInstrument.Sessions == null) TheInstrument.Sessions = new List<InstrumentSession>();
                TheInstrument.Sessions = TheInstrument.Sessions.OrderBy(x => x.OpeningDay).ThenBy(x => x.OpeningTime).ToList();
            }
            else
            {
                TheInstrument = new Instrument 
                {
                    Tags = new List<Tag>(), 
                    Sessions = new List<InstrumentSession>()
                };
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

                TheInstrument.Tags.Clear();
            
                foreach (Tag t in Tags.Where(x=> x.IsChecked).Select(x => x.Item))
                {
                    context.Tags.Attach(t);
                    TheInstrument.Tags.Add(t);
                }


                if (_addingNew)
                {
                    if (TheInstrument.Exchange != null) context.Exchanges.Attach(TheInstrument.Exchange);
                    if (TheInstrument.PrimaryExchange != null) context.Exchanges.Attach(TheInstrument.PrimaryExchange);
                    context.Datasources.Attach(TheInstrument.Datasource);

                    context.Instruments.Add(TheInstrument);
                }
                else //simply manipulating an existing instrument
                {
                    context.Exchanges.Attach(TheInstrument.Exchange);
                    context.Exchanges.Attach(TheInstrument.PrimaryExchange);
                    context.Datasources.Attach(TheInstrument.Datasource);

                    context.Instruments.Attach(_originalInstrument);
                    context.Entry(_originalInstrument).CurrentValues.SetValues(TheInstrument);
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

            TheInstrument.Sessions.Clear();
            TheInstrument.SessionsSource = SessionsSource.Exchange;
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
            foreach (TemplateSession s in template.Sessions)
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
    }
}
