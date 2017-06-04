// -----------------------------------------------------------------------
// <copyright file="TagModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using System.Data.Entity;
using System.Linq;
using System.Threading;

namespace QDMS.Server.NancyModules
{
    public class TagModule : NancyModule
    {
        public TagModule(IMyDbContext context) : base("/tags")
        {
            this.RequiresAuthentication();

            Get["/", true] = async (_, token) => await context.Tags.ToListAsync(token).ConfigureAwait(false);

            Post["/"] = _ =>
            {
                var tag = this.BindAndValidate<Tag>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                //check for existing tag
                var existingTag = context.Tags.FirstOrDefault(x => x.Name == tag.Name);
                if (existingTag != null) return HttpStatusCode.Conflict;

                context.Tags.Add(tag);
                context.SaveChanges();

                //return the object with the id after inserting
                return tag;
            };

            Put["/"] = _ =>
            {
                var newTag = this.BindAndValidate<Tag>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                //make sure the tag we want to update exists
                var tag = context.Tags.FirstOrDefault(x => x.ID == newTag.ID);
                if (tag == null) return HttpStatusCode.NotFound;

                //update values
                context.UpdateEntryValues(tag, newTag);

                context.SaveChanges();

                return tag;
            };

            Delete["/{Id:int}"] = parameters =>
            {
                int id = parameters.Id;
                var tag = context.Tags.FirstOrDefault(x => x.ID == id);

                if (tag == null) return HttpStatusCode.NotFound;

                context.Tags.Remove(tag);
                context.SaveChanges();

                return tag;
            };
        }
    }
}