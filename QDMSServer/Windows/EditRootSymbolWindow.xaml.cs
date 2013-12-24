// -----------------------------------------------------------------------
// <copyright file="EditRootSymbolWindow.xaml.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using System.Windows;
using EntityData;
using MahApps.Metro.Controls;
using QDMS;

namespace QDMSServer
{
    /// <summary>
    /// Interaction logic for EditRootSymbolWindow.xaml
    /// </summary>
    public partial class EditRootSymbolWindow : MetroWindow
    {
        public UnderlyingSymbol TheSymbol { get; set; }
        public bool SymbolAdded;

        private UnderlyingSymbol _originalSymbol;

        public EditRootSymbolWindow(UnderlyingSymbol symbol)
        {
            InitializeComponent();
            DataContext = this;

            if (symbol == null)
            {
                TheSymbol = new UnderlyingSymbol 
                {
                    Rule = new ExpirationRule(), 
                    ID = -1
                };
                ModifyBtn.Content = "Add";
            }
            else
            {
                _originalSymbol = symbol;
                TheSymbol = (UnderlyingSymbol)symbol.Clone();
                ModifyBtn.Content = "Modify";
            }

            //set the corrent radio box check
            if (TheSymbol.Rule.ReferenceDayIsLastBusinessDayOfMonth)
            {
                LastBusinessDayRadioBtn.IsChecked = true;
            }
            else if (TheSymbol.Rule.ReferenceUsesDays)
            {
                DaysBasedRefCheckBox.IsChecked = true;
            }
            else
            {
                WeeksBasedRefCheckBox.IsChecked = true;
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            SymbolAdded = false;
            Hide();
        }

        private void ModifyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TheSymbol.Symbol))
            {
                MessageBox.Show("Must have a symbol.");
                return;
            }

            
            using (var entityContext = new MyDBContext())
            {
                //check that the symbol doesn't already exist
                bool symbolExists = entityContext.UnderlyingSymbols.Count(x => x.Symbol == TheSymbol.Symbol) > 0;
                bool addingNew = TheSymbol.ID == -1;

                if (symbolExists && addingNew)
                {
                    MessageBox.Show("Must have a symbol.");
                    return;
                }

                if (addingNew)
                {
                    entityContext.UnderlyingSymbols.Add(TheSymbol);
                }
                else
                {
                    entityContext.UnderlyingSymbols.Attach(_originalSymbol);
                    entityContext.Entry(_originalSymbol).CurrentValues.SetValues(TheSymbol);
                }

                entityContext.SaveChanges();
            }

            SymbolAdded = true;
            Hide();
        }

        private void DaysBasedRefCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TheSymbol.Rule.ReferenceUsesDays = true;
            TheSymbol.Rule.ReferenceDayIsLastBusinessDayOfMonth = false;
        }

        private void WeeksBasedRefCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            TheSymbol.Rule.ReferenceUsesDays = false;
            TheSymbol.Rule.ReferenceDayIsLastBusinessDayOfMonth = false;
        }

        private void LastBusinessDayRadioBtn_Checked(object sender, RoutedEventArgs e)
        {
            TheSymbol.Rule.ReferenceDayIsLastBusinessDayOfMonth = true;
        }
    }
}
