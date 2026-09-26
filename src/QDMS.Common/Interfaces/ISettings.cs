// -----------------------------------------------------------------------
// <copyright file="ISettings.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable 1591

namespace QDMS
{
    /// <summary>
    /// Server settings. Bound from the "Qdms" configuration section (appsettings.json / environment variables).
    /// </summary>
    public interface ISettings
    {
        // Interactive Brokers
        string ibClientHost { get; }
        int ibClientPort { get; }
        bool ibUseNewRealTimeDataSystem { get; }
        int histClientIBID { get; }
        int rtdClientIBID { get; }

        // Servers
        int rtDBPubPort { get; }
        int rtDBReqPort { get; }
        int hDBPort { get; }
        int httpPort { get; }
        string apiKey { get; }

        // Data sources
        string tiingoApiKey { get; }
        string EconomicReleaseDefaultDatasource { get; }

        // Update job reporting
        bool updateJobReportErrors { get; }
        bool updateJobReportOutliers { get; }
        bool updateJobReportNoData { get; }
        bool updateJobTimeouts { get; }
        int updateJobTimeout { get; }
        string updateJobEmail { get; }
        string updateJobEmailHost { get; }
        string updateJobEmailUsername { get; }
        string updateJobEmailPassword { get; }
        int updateJobEmailPort { get; }
        string updateJobEmailSender { get; }
    }
}
