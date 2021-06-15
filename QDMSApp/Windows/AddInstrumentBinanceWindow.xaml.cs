using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using QDMS;
using QDMSApp.ViewModels;
using System.Reactive.Linq;
using System.Windows;

namespace QDMSApp
{
    /// <summary>
    /// Interaction logic for AddInstrumentBinanceWindow.xaml
    /// </summary>
    public partial class AddInstrumentBinanceWindow : MetroWindow
    {
        public AddInstrumentBinanceViewModel ViewModel { get; set; }

        public AddInstrumentBinanceWindow(IDataClient client)
        {
            InitializeComponent();

            ViewModel = new AddInstrumentBinanceViewModel(client, DialogCoordinator.Instance);
            DataContext = ViewModel;
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.Load.Execute();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
