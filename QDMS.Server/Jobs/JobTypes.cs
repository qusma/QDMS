// -----------------------------------------------------------------------
// <copyright file="JobTypes.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

namespace QDMS.Server.Jobs
{
    public static class JobTypes
    {
        public const string DataUpdate = "DataUpdate";

        public static string GetJobType(IJobDetails job)
        {
            if (job is DataUpdateJobDetails) return DataUpdate;

            return "";
        }
    }
}
