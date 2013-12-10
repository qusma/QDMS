// -----------------------------------------------------------------------
// <copyright file="NLogUtils.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NLog;

namespace QDMSServer
{
    public static class NLogUtils
    {
        public static void Reconfigure(LogLevel logLevel)
        {
            foreach (var rule in LogManager.Configuration.LoggingRules)
            {
                rule.EnableLoggingForLevel(logLevel);
            }

            //Call to update existing Loggers created with GetLogger() or 
            //GetCurrentClassLogger()
            LogManager.ReconfigExistingLoggers();
        }
    }
}
