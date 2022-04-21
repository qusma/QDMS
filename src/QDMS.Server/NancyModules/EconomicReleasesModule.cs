// -----------------------------------------------------------------------
// <copyright file="EconomicReleasesModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using QDMS.Server.Brokers;
using System.Data.Entity;
using System.Linq;

namespace QDMS.Server.NancyModules
{
    public class EconomicReleasesModule : NancyModule
    {
        public EconomicReleasesModule(IMyDbContext context, IEconomicReleaseBroker erb) : base("/economicreleases")
        {
            this.RequiresAuthentication();

            Get("/", async (_, token) =>
            {
                var releases = context.EconomicReleases;

                var request = this.Bind<EconomicReleaseRequest>();

                if (request == null)
                {
                    // No request object, just return everything
                    return await releases.ToListAsync(token).ConfigureAwait(false);
                }

                // filter and return
                return await erb.RequestEconomicReleases(request).ConfigureAwait(false);
            });

            Get("/datasources", _ => erb.DataSources.Keys.ToList());
        }
    }
}