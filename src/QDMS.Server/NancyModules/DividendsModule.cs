// -----------------------------------------------------------------------
// <copyright file="DividendsModule.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using QDMS.Server.Brokers;
using System.Linq;

namespace QDMS.Server.NancyModules
{
    public class DividendsModule : NancyModule
    {
        public DividendsModule(IDividendsBroker broker)
            : base("/dividends")
        {
            this.RequiresAuthentication();

            Get("/", async (_, token) =>
            {
                var request = this.Bind<DividendRequest>();

                if (request == null) return HttpStatusCode.BadRequest;

                return await broker.RequestDividends(request).ConfigureAwait(false);
            });

            Get("/datasources", _ => broker.DataSources.Keys.ToList());
        }
    }
}
