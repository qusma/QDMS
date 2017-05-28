// -----------------------------------------------------------------------
// <copyright file="AddInstrumentManuallyWindow.xaml.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSServer.ViewModels;

namespace QDMSServer
{
    public partial class AddInstrumentManuallyWindow : IClosableView
    {
        public ObservableCollection<Exchange> Exchanges { get; set; }
        public ObservableCollection<KeyValuePair<int, string>> ContractMonths { get; set; }
        public EditInstrumentViewModel ViewModel { get; }

        ///  <summary>
        ///
        ///  </summary>
        /// <param name="client"></param>
        /// <param name="instrument">If we're updating or cloning an instrument, pass it here.</param>
        ///  <param name="addingNew">True if adding a new instrument. False if we're updating an instrument.</param>
        ///  <param name="addingContFut">True if adding a continuous futures instrument.</param>
        public AddInstrumentManuallyWindow(IDataClient client, Instrument instrument = null, bool addingNew = true, bool addingContFut = false)
        {
            InitializeComponent();
            //make a clone for the editing
            Instrument inst;
            if (addingNew)
            {
                if (instrument == null)
                {
                    //brand new instrument
                    inst = new Instrument();
                    if (addingContFut)
                    {
                        inst.IsContinuousFuture = true;
                        inst.Type = InstrumentType.Future;
                        inst.ContinuousFuture = new ContinuousFuture();
                    }
                }
                else
                {
                    //in this case we are cloning an existing instrument
                    inst = (Instrument)instrument.Clone();
                    inst.ID = null;
                }
            }
            else
            {
                inst = (Instrument)instrument.Clone();
            }

            ViewModel = new EditInstrumentViewModel(inst, client, this, DialogCoordinator.Instance);
            DataContext = ViewModel;

            //Window title
            if (addingNew)
            {
                Title = "Add New Instrument";
                AddBtn.Content = "Add";
            }
            else
            {
                Title = "Modify Instrument";
                AddBtn.Content = "Modify";
            }

            //sort the sessions so they're ordered properly...
            SessionsGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("OpeningDay", System.ComponentModel.ListSortDirection.Ascending));

            //fill the RolloverRuleType combobox
            var rolloverTypes = MyUtils.GetEnumValues<ContinuousFuturesRolloverType>();
            foreach (ContinuousFuturesRolloverType t in rolloverTypes)
            {
                if (t != ContinuousFuturesRolloverType.Time)
                    RolloverRuleType.Items.Add(t);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void MetroWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            await ViewModel.Load.Execute();
        }
    }
}