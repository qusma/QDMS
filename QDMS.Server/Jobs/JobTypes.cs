// -----------------------------------------------------------------------
// <copyright file="JobTypes.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS.Server.Jobs.JobDetails;

namespace QDMS.Server.Jobs
{
    public static class JobTypes
    {
        public const string DataUpdate = "DataUpdate";
        public const string EconomicRelease = "EconomicRelease";

        public static string GetJobType(IJobSettings job)
        {
            if (job is DataUpdateJobSettings) return DataUpdate;
            if (job is EconomicReleaseUpdateJobSettings) return EconomicRelease;

            return "";
        }
    }
}
