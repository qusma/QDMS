// -----------------------------------------------------------------------
// <copyright file="QdmsSettings.cs" company="">
// Copyright 2026 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS.Host
{
    /// <summary>
    /// The "Qdms" configuration section. Override any value with environment variables,
    /// e.g. Qdms__apiKey or Qdms__ibClientHost.
    /// </summary>
    public class QdmsSettings : ISettings
    {
        public string ibClientHost { get; set; } = "127.0.0.1";
        public int ibClientPort { get; set; } = 4002;
        public bool ibUseNewRealTimeDataSystem { get; set; }
        public int histClientIBID { get; set; } = 99;
        public int rtdClientIBID { get; set; } = 98;

        public int rtDBPubPort { get; set; } = 5556;
        public int rtDBReqPort { get; set; } = 5557;
        public int hDBPort { get; set; } = 5555;
        public int httpPort { get; set; } = 5559;
        public string apiKey { get; set; }

        public string tiingoApiKey { get; set; }
        public string EconomicReleaseDefaultDatasource { get; set; }

        public bool updateJobReportErrors { get; set; } = true;
        public bool updateJobReportOutliers { get; set; } = true;
        public bool updateJobReportNoData { get; set; } = true;
        public bool updateJobTimeouts { get; set; } = true;
        public int updateJobTimeout { get; set; } = 5;
        public string updateJobEmail { get; set; }
        public string updateJobEmailHost { get; set; }
        public string updateJobEmailUsername { get; set; }
        public string updateJobEmailPassword { get; set; }
        public int updateJobEmailPort { get; set; } = 587;
        public string updateJobEmailSender { get; set; }

        /// <summary>
        /// Interactive Brokers needs a running TWS/IB Gateway; leave it off otherwise, or it keeps retrying the connection.
        /// </summary>
        public bool enableInteractiveBrokers { get; set; }
    }
}
