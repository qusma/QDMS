// -----------------------------------------------------------------------
// <copyright file="EditSessionTemplateWindow.xaml.cs" company="">
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
    /// Interaction logic for EditSessionTemplateWindow.xaml
    /// </summary>
    public partial class EditSessionTemplateWindow : MetroWindow
    {
        public SessionTemplate TheTemplate { get; set; }
        public bool TemplateAdded;

        private readonly SessionTemplate _originalTemplate;

        public EditSessionTemplateWindow(SessionTemplate template)
        {
            InitializeComponent();
            DataContext = this;


            if (template == null)
            {
                TheTemplate = new SessionTemplate { ID = -1 };
                TheTemplate.Sessions = new List<TemplateSession>();
                ModifyBtn.Content = "Add";
            }
            else
            {
                template.Sessions = template.Sessions.OrderBy(x => x.OpeningDay).ThenBy(x => x.OpeningTime).ToList();
                _originalTemplate = template;
                TheTemplate = (SessionTemplate)template.Clone();
                ModifyBtn.Content = "Modify";
            }
        }

        private void ModifyBtn_Click(object sender, RoutedEventArgs e)
        {
            //ensure sessions don't overlap
            List<string> error;
            if (!MyUtils.ValidateSessions(TheTemplate.Sessions.ToList(), out error))
            {
                MessageBox.Show(error.First());
                return;
            }

            //save to db
            using (var entityContext = new MyDBContext())
            {
                bool nameExists = entityContext.SessionTemplates.Any(x => x.Name == TheTemplate.Name);
                bool addingNew = TheTemplate.ID == -1;

                if (nameExists && (addingNew || _originalTemplate.Name != TheTemplate.Name))
                {
                    MessageBox.Show("Name already exists, please change it.");
                    return;
                }

                if (addingNew)
                {
                    entityContext.SessionTemplates.Add(TheTemplate);
                }
                else
                {
                    entityContext.SessionTemplates.Attach(_originalTemplate);
                    entityContext.Entry(_originalTemplate).CurrentValues.SetValues(TheTemplate);
                }

                entityContext.SaveChanges();

                //find removed sessions and mark them as deleted
                if (_originalTemplate != null)
                {
                    var removedSessions = _originalTemplate.Sessions.Where(x => !TheTemplate.Sessions.Any(y => y.ID == x.ID)).ToList();
                    foreach (TemplateSession t in removedSessions)
                    {
                        entityContext.Entry(t).State = System.Data.Entity.EntityState.Deleted;
                    }


                    //find the ones that overlap and modify them, if not add them
                    foreach (TemplateSession s in TheTemplate.Sessions)
                    {
                        if (s.ID != 0) //this means it's not newly added
                        {
                            var session = _originalTemplate.Sessions.First(x => x.ID == s.ID);
                            entityContext.TemplateSessions.Attach(session);
                            entityContext.Entry(session).CurrentValues.SetValues(s);
                        }
                    }
                }

                entityContext.SaveChanges();

                //find instruments using this exchange as session source, and update their sessions
                if (TheTemplate.ID != -1)
                {
                    var instruments = entityContext.Instruments.Where(x => x.SessionsSource == SessionsSource.Template && x.ExchangeID == TheTemplate.ID).ToList();
                    foreach (Instrument i in instruments)
                    {
                        entityContext.InstrumentSessions.RemoveRange(i.Sessions);
                        i.Sessions.Clear();

                        foreach (TemplateSession s in TheTemplate.Sessions)
                        {
                            i.Sessions.Add(s.ToInstrumentSession());
                        }
                    }
                }

                entityContext.SaveChanges();
            }
            TemplateAdded = true;

            Hide();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            TemplateAdded = false;
            Hide();
        }

        private void AddSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            var toAdd = new TemplateSession();
            toAdd.IsSessionEnd = true;

            if (TheTemplate.Sessions.Count == 0)
            {
                toAdd.OpeningDay = DayOfTheWeek.Monday;
                toAdd.ClosingDay = DayOfTheWeek.Monday;
            }
            else
            {
                DayOfTheWeek maxDay = (DayOfTheWeek)Math.Min(6, TheTemplate.Sessions.Max(x => (int)x.OpeningDay) + 1);
                toAdd.OpeningDay = maxDay;
                toAdd.ClosingDay = maxDay;
            }
            TheTemplate.Sessions.Add(toAdd);
            SessionsGrid.ItemsSource = null;
            SessionsGrid.ItemsSource = TheTemplate.Sessions;
        }

        private void RemoveSessionBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedSession = (TemplateSession)SessionsGrid.SelectedItem;
            TheTemplate.Sessions.Remove(selectedSession);

            SessionsGrid.ItemsSource = null;
            SessionsGrid.ItemsSource = TheTemplate.Sessions;
        }
    }
}
