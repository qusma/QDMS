// -----------------------------------------------------------------------
// <copyright file="EditExchangeWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EntityData;
using MahApps.Metro.Controls;
using QDMS;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for EditExchangeWindow.xaml
    /// </summary>
    public partial class EditExchangeWindow : MetroWindow
    {
        public Exchange TheExchange { get; set; }
        public bool ExchangeAdded;

        private readonly bool _addingNew;
        private readonly Exchange _originalExchange;

        public EditExchangeWindow(Exchange exchange)
        {
            InitializeComponent();
            DataContext = this;

            if (exchange == null)
            {
                TheExchange = new Exchange {ID = -1};
                _addingNew = true;

                ModifyBtn.Content = "Add";
            }
            else
            {
                exchange.Sessions = exchange.Sessions.OrderBy(x => x.OpeningDay).ThenBy(x => x.OpeningTime).ToList();
                _originalExchange = exchange;
                TheExchange = (Exchange)exchange.Clone();
                ModifyBtn.Content = "Modify";
            }

            var timezones = TimeZoneInfo.GetSystemTimeZones();
            foreach (TimeZoneInfo tz in timezones)
            {
                TimeZoneComboBox.Items.Add(tz);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ExchangeAdded = false;
            Hide();
        }

        private void ModifyBtn_Click(object sender, RoutedEventArgs e)
        {
            //ensure sessions don't overlap
            List<string> error;
            if (!MyUtils.ValidateSessions(TheExchange.Sessions.ToList(), out error))
            {
                MessageBox.Show(error.First());
                return;
            }

            //save to db
            using (var entityContext = new MyDBContext())
            {
                bool nameExists = entityContext.Exchanges.Any(x => x.Name == TheExchange.Name);
                if (nameExists && (_addingNew || _originalExchange.Name != TheExchange.Name))
                {
                    MessageBox.Show("Name already exists, please change it.");
                    return;
                }

                if (_addingNew)
                {
                    entityContext.Exchanges.Add(TheExchange);
                }
                else
                {
                    entityContext.Exchanges.Attach(_originalExchange);
                    entityContext.Entry(_originalExchange).CurrentValues.SetValues(TheExchange);
                }

                entityContext.SaveChanges();

                //find removed sessions and mark them as deleted
                var removedSessions = _originalExchange.Sessions.Where(x => !TheExchange.Sessions.Any(y => y.ID == x.ID)).ToList();
                for (int i = 0; i < removedSessions.Count; i++)
                {
                    entityContext.Entry(removedSessions[i]).State = System.Data.Entity.EntityState.Deleted;
                }

                //find the ones that overlap and modify them, if not add them
                foreach (ExchangeSession s in TheExchange.Sessions)
                {
                    if (s.ID != 0) //this means it's not newly added
                    {
                        var session = _originalExchange.Sessions.First(x => x.ID == s.ID);
                        entityContext.ExchangeSessions.Attach(session);
                        entityContext.Entry(session).CurrentValues.SetValues(s);
                    }
                    else //completely new
                    {
                        _originalExchange.Sessions.Add(s);
                    }
                }

                entityContext.SaveChanges();

                //find instruments using this exchange as session source, and update their sessions
                if (TheExchange.ID != -1)
                {
                    var instruments = entityContext.Instruments.Where(x => x.SessionsSource == SessionsSource.Exchange && x.ExchangeID == TheExchange.ID).ToList();
                    foreach (Instrument i in instruments)
                    {
                        entityContext.InstrumentSessions.RemoveRange(i.Sessions);
                        i.Sessions.Clear();
                    
                        foreach (ExchangeSession s in TheExchange.Sessions)
                        {
                            i.Sessions.Add(s.ToInstrumentSession());
                        }
                    }
                }

                entityContext.SaveChanges();
            }
            ExchangeAdded = true;

            Hide();
        }

        private void AddSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            var toAdd = new ExchangeSession();
            toAdd.IsSessionEnd = true;

            if (TheExchange.Sessions.Count == 0)
            {
                toAdd.OpeningDay = DayOfTheWeek.Monday;
                toAdd.ClosingDay = DayOfTheWeek.Monday;
            }
            else
            {
                DayOfTheWeek maxDay = (DayOfTheWeek)Math.Min(6, TheExchange.Sessions.Max(x => (int)x.OpeningDay) + 1);
                toAdd.OpeningDay = maxDay;
                toAdd.ClosingDay = maxDay;
            }
            TheExchange.Sessions.Add(toAdd);
            SessionsGrid.ItemsSource = null;
            SessionsGrid.ItemsSource = TheExchange.Sessions;
        }

        private void RemoveSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedSession = (ExchangeSession)SessionsGrid.SelectedItem;
            TheExchange.Sessions.Remove(selectedSession);

            SessionsGrid.ItemsSource = null;
            SessionsGrid.ItemsSource = TheExchange.Sessions;
        }
    }
}
