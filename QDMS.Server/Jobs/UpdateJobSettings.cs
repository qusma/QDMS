// -----------------------------------------------------------------------
// <copyright file="UpdateJobErrorReportingSettings.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMSServer
{
    public class UpdateJobSettings
    {
        /// <summary>
        /// Send a notification when no new data was received.
        /// </summary>
        public bool NoDataReceived { get; set; }

        /// <summary>
        /// Send a notification when there were errors during the update.
        /// </summary>
        public bool Errors { get; set; }

        /// <summary>
        /// Send a notification if there were outliers detected in the data.
        /// </summary>
        public bool Outliers { get; set; }

        /// <summary>
        /// Send a notification when requests are still pending when the job times out.
        /// </summary>
        public bool RequestTimeouts { get; set; }

        public int Timeout { get; set; }

        public string ToEmail { get; set; }

        public string FromEmail { get; set; }

        public UpdateJobSettings(
            bool noDataReceived = true, 
            bool errors = true, 
            bool outliers = true, 
            bool requestTimeouts = true,
            int timeout = 5,
            string toEmail = "",
            string fromEmail = "")
        {
            NoDataReceived = noDataReceived;
            Errors = errors;
            Outliers = outliers;
            RequestTimeouts = requestTimeouts;
            Timeout = timeout;
            ToEmail = toEmail;
            FromEmail = fromEmail;
        }
    }
}
