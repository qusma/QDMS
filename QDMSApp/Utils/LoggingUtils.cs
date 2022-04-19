using NLog;
using NLog.Targets;
using System.IO;


namespace QDMSApp.Utils
{
    internal static class LoggingUtils
    {
        internal static void SetLogDirectory()
        {
            if (Directory.Exists(QDMSApp.Properties.Settings.Default.logDirectory))
            {
                ((FileTarget)LogManager.Configuration.FindTargetByName("default")).FileName = QDMSApp.Properties.Settings.Default.logDirectory + "Log.log";
            }
        }
    }
}
