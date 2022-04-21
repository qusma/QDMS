// -----------------------------------------------------------------------
// <copyright file="DatasourceModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Nancy;
using Nancy.Security;
using QDMS.Server.Services;

namespace QDMS.Server.NancyModules
{
    public class DatasourceModule : NancyModule
    {
        public DatasourceModule(IDatasourceService dsService)
            : base("/datasources")
        {
            this.RequiresAuthentication();

            Get("/", async (_, token) => await dsService.GetAll(token));

            Get("/status", _ => dsService.GetDatasourceStatus());

            Get("/activestreams", _ => dsService.GetActiveStreams());
        }
    }
}