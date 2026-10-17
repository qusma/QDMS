// -----------------------------------------------------------------------
// <copyright file="SessionTemplateEndpoints.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;

namespace QDMS.Server.Http
{
    public static class SessionTemplateEndpoints
    {
        public static void MapSessionTemplateEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/sessiontemplates");

            group.MapGet("/", async (IMyDbContext context) =>
                HttpApi.Json(await context.SessionTemplates.Include(x => x.Sessions).ToListAsync().ConfigureAwait(false)));

            group.MapPost("/", async (HttpContext http, IMyDbContext context) =>
            {
                var (template, failure) = await http.ReadAndValidateBody<SessionTemplate>().ConfigureAwait(false);
                if (failure != null) return failure;

                context.SessionTemplates.Add(template);
                await context.SaveChangesAsync().ConfigureAwait(false);

                //return the object with the id after inserting
                return HttpApi.Json(template);
            });

            group.MapPut("/", async (HttpContext http, IMyDbContext context) =>
            {
                var (newValues, failure) = await http.ReadAndValidateBody<SessionTemplate>().ConfigureAwait(false);
                if (failure != null) return failure;

                //make sure the template we want to update exists
                var template = await context.SessionTemplates.Include(x => x.Sessions).FirstOrDefaultAsync(x => x.ID == newValues.ID).ConfigureAwait(false);
                if (template == null) return HttpApi.NotFound();

                //update values on the template
                context.UpdateEntryValues(template, newValues);

                //add/remove/update the Sessions collection
                template.Sessions.UpdateCollectionAndElements(newValues.Sessions, context);

                //some instruments may have their sessions based on this template, we need to update them
                var instruments = await context.Instruments
                    .Include(x => x.Sessions)
                    .Where(x => x.SessionsSource == SessionsSource.Template && x.SessionTemplateID == template.ID)
                    .ToListAsync().ConfigureAwait(false);

                foreach (Instrument i in instruments)
                {
                    context.InstrumentSessions.RemoveRange(i.Sessions);
                    i.Sessions.Clear();

                    foreach (TemplateSession s in template.Sessions)
                    {
                        i.Sessions.Add(s.ToInstrumentSession());
                    }
                }

                await context.SaveChangesAsync().ConfigureAwait(false);

                return HttpApi.Json(template);
            });

            group.MapDelete("/{id:int}", async (int id, IMyDbContext context) =>
            {
                var template = await context.SessionTemplates.FirstOrDefaultAsync(x => x.ID == id).ConfigureAwait(false);
                if (template == null) return HttpApi.NotFound();

                //make sure there are no references to it
                var templateReferenced = await context.Instruments
                    .AnyAsync(x => x.SessionTemplateID == id && x.SessionsSource == SessionsSource.Template).ConfigureAwait(false);
                if (templateReferenced)
                {
                    return HttpApi.Error(HttpStatusCode.Conflict, "Can't delete this template because it has instruments assigned to it.");
                }

                context.SessionTemplates.Remove(template);
                await context.SaveChangesAsync().ConfigureAwait(false);
                return HttpApi.Json(template);
            });
        }
    }
}
