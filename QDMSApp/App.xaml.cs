using Common.Logging.NLog;
using NLog;
using QDMSApp.Utils;
using System;
using System.Windows;

namespace QDMSApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Common.Logging.LogManager.Adapter = new NLogLoggerFactoryAdapter(new Common.Logging.Configuration.NameValueCollection());

            //Prompt for db settings if not found, we need them before we can do anything
            DBUtils.CheckDBConnection();

            //set the log directory
            LoggingUtils.SetLogDirectory();

            //Log unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += AppDomain_CurrentDomain_UnhandledException;

            //set the connection string
            DBUtils.SetConnectionString();

            //set EF configuration, necessary for MySql to work
            DBUtils.SetDbConfiguration();

            //database creation/migration
            DBUtils.CreateDatabases();

            //set up DI
            var container = DependencyInjection.ComposeObjects();

            //and go!
            var mainWindow = container.GetInstance<MainWindow>();
            mainWindow.Show();
        }

        

        private void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Error((Exception)e.ExceptionObject, "Unhandled exception");
        }
    }
}
