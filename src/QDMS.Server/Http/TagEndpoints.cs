// -----------------------------------------------------------------------
// <copyright file="TagEndpoints.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace QDMS.Server.Http
{
    public static class TagEndpoints
    {
        public static void MapTagEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/tags");

            group.MapGet("/", async (IMyDbContext context) =>
                HttpApi.Json(await context.Tags.ToListAsync().ConfigureAwait(false)));

            group.MapPost("/", async (HttpContext http, IMyDbContext context) =>
            {
                var (tag, failure) = await http.ReadAndValidateBody<Tag>().ConfigureAwait(false);
                if (failure != null) return failure;

                //check for existing tag
                if (await context.Tags.AnyAsync(x => x.Name == tag.Name).ConfigureAwait(false))
                {
                    return HttpApi.Error(HttpStatusCode.Conflict, "A tag with this name already exists");
                }

                context.Tags.Add(tag);
                await context.SaveChangesAsync().ConfigureAwait(false);

                //return the object with the id after inserting
                return HttpApi.Json(tag);
            });

            group.MapPut("/", async (HttpContext http, IMyDbContext context) =>
            {
                var (newTag, failure) = await http.ReadAndValidateBody<Tag>().ConfigureAwait(false);
                if (failure != null) return failure;

                //make sure the tag we want to update exists
                var tag = await context.Tags.FirstOrDefaultAsync(x => x.ID == newTag.ID).ConfigureAwait(false);
                if (tag == null) return HttpApi.NotFound();

                context.UpdateEntryValues(tag, newTag);
                await context.SaveChangesAsync().ConfigureAwait(false);

                return HttpApi.Json(tag);
            });

            group.MapDelete("/{id:int}", async (int id, IMyDbContext context) =>
            {
                var tag = await context.Tags.FirstOrDefaultAsync(x => x.ID == id).ConfigureAwait(false);
                if (tag == null) return HttpApi.NotFound();

                context.Tags.Remove(tag);
                await context.SaveChangesAsync().ConfigureAwait(false);

                return HttpApi.Json(tag);
            });
        }
    }
}
