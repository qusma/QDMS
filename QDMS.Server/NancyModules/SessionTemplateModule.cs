// -----------------------------------------------------------------------
// <copyright file="SessionTemplateModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using QDMS.Server.Nancy;
using System.Data.Entity;
using System.Linq;

namespace QDMS.Server.NancyModules
{
    public class SessionTemplateModule : NancyModule
    {
        public SessionTemplateModule(IMyDbContext context) : base("/sessiontemplates")
        {
            this.RequiresAuthentication();

            var dbSet = context.Set<SessionTemplate>();

            Get["/", runAsync: true] = async (_, token) => await dbSet.Include(x => x.Sessions).ToListAsync(token).ConfigureAwait(false);

            Post["/"] = _ =>
            {
                SessionTemplate template = this.BindAndValidate<SessionTemplate>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                dbSet.Add(template);
                context.SaveChanges();

                //return the object with the id after inserting
                return template;
            };

            Put["/"] = _ =>
            {
                SessionTemplate newValues = this.BindAndValidate<SessionTemplate>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                //make sure the template we want to update exists
                var template = dbSet.Include(x => x.Sessions).FirstOrDefault(x => x.ID == newValues.ID);
                if (template == null) return HttpStatusCode.NotFound;

                //update values on the template
                context.UpdateEntryValues(template, newValues);

                //add/remove/update the Sessions collection
                template.Sessions.UpdateCollectionAndElements(newValues.Sessions, context);

                //some instruments may have their sessions based on this template, we need to update them
                var instruments = context.Set<Instrument>()
                    .Where(x => x.SessionsSource == SessionsSource.Template && x.SessionTemplateID == template.ID).ToList();
                foreach (Instrument i in instruments)
                {
                    context.Set<InstrumentSession>().RemoveRange(i.Sessions);
                    i.Sessions.Clear();

                    foreach (TemplateSession s in template.Sessions)
                    {
                        i.Sessions.Add(s.ToInstrumentSession());
                    }
                }

                context.SaveChanges();

                return template;
            };

            Delete["/{Id:int}"] = parameters =>
            {
                //It's possible to delete
                int id = parameters.Id;
                var template = dbSet.FirstOrDefault(x => x.ID == id);

                if (template == null) return HttpStatusCode.NotFound;

                //make sure there are no references to it
                var templateReferenced = context.Set<Instrument>().Any(x => x.SessionTemplateID == id && x.SessionsSource == SessionsSource.Template);
                if (templateReferenced)
                {
                    return Negotiate
                        .WithModel(new ErrorResponse(
                            HttpStatusCode.Conflict,
                            "Can't delete this template because it has instruments assigned to it.", ""))
                        .WithStatusCode(HttpStatusCode.Conflict);
                }

                dbSet.Remove(template);
                context.SaveChanges();
                return template;
            };
        }
    }
}