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
        public const string EconomicRelease = "EconomicRelease";
        public const string DividendUpdate = "DividendUpdate";

        public static string GetJobType(IJobSettings job)
        {
            if (job is DataUpdateJobSettings) return DataUpdate;
            if (job is EconomicReleaseUpdateJobSettings) return EconomicRelease;
            if (job is DividendUpdateJobSettings) return DividendUpdate;

            return "";
        }
    }
}