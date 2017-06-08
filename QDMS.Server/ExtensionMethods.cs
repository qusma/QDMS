// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using EntityData;
using Nancy;
using Nancy.Responses.Negotiation;
using QDMS.Server.Nancy;
using MySql.Data.MySqlClient;

namespace QDMS.Server
{
    public static class ExtensionMethods
    {
        public static Negotiator ValidationFailure(this NancyModule module)
        {
            return module.Negotiate
                .WithModel(new ValidationErrorResponse(module.ModelValidationResult))
                .WithStatusCode(HttpStatusCode.BadRequest);
        }

        public static Negotiator ValidationFailure(this NancyModule module, string error)
        {
            return module.Negotiate
                .WithModel(new ValidationErrorResponse(error))
                .WithStatusCode(HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Updates a collection, adding/removing values. 
        /// All newValues must already exist in the database.
        /// Does not update the fields of each element.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="newValues"></param>
        /// <param name="context"></param>
        public static void UpdateCollection<T>(this ICollection<T> collection, IEnumerable<T> newValues, IMyDbContext context) where T : class, IEntity
        {
            var comparer = new LambdaEqualityComparer<T>((x, y) => x.ID == y.ID, x => x.ID);
            var toAdd = newValues.Except(collection, comparer).ToList();
            var toRemove = collection.Except(newValues, comparer).ToList();
            //grab everything so we don't need an individual query for every item
            var allItems = context.Set<T>().ToList().ToDictionary(x => x.ID, x => x);

            foreach (var element in toAdd)
            {
                var entry = allItems[element.ID];
                collection.Add(entry);
            }
            foreach (var element in toRemove)
            {
                var entry = allItems[element.ID];
                collection.Remove(entry);
            }
        }

        /// <summary>
        /// Updates a collection, adding/removing values. The entities in the collection are assumed to be attached/tracked.
        /// newValues need not already exist.
        /// Also updates the fields of each element.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="newValues"></param>
        /// <param name="context"></param>
        public static void UpdateCollectionAndElements<T>(this ICollection<T> collection, IEnumerable<T> newValues, IMyDbContext context) where T : class, IEntity
        {
            //remove those that need to be removed
            var toRemove = collection.Where(x => !newValues.Any(y => y.ID == x.ID)).ToList();
            foreach (T item in toRemove)
            {
                context.SetEntryState(item, EntityState.Deleted);
            }

            //find the ones that overlap and add or update them
            foreach (T item in newValues)
            {
                if (item.ID == 0) //this means it's newly added
                {
                    collection.Add(item);
                }
                else //this is an existing entity, update it
                {
                    var attached = collection.FirstOrDefault(x => x.ID == item.ID); //no need for Entry()/Attach() -  these are already in the ObjectStateManager
                    if (attached == null) continue; //if the collection on the server has been changed and the client tries to update a deleted element, you can end up in this scenario...just skip it
                    context.UpdateEntryValues(attached, item);
                }
            }
        }

        /// <summary>
        /// Tries to get an entity from the Local collection, without hitting the database
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static T GetLocal<T>(this IMyDbContext context, T entity) where T : class, IEntity
        {
            if (entity == null) return null;

            return context.Set<T>().Local.FirstOrDefault(x => x.ID == entity.ID);
        }

        /// <summary>
        /// Gets an attached entity from the ID of a detached entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static T GetAttachedEntity<T>(this IMyDbContext context, T entity) where T : class, IEntity
        {
            if (entity == null) return null;

            return context.GetLocal(entity) ?? context.Set<T>().FirstOrDefault(x => x.ID == entity.ID);
        }

        public static bool IsUniqueKeyException(this SqlException ex)
        {
            return ex.Errors.Cast<SqlError>().Any(x => x.Number == 2601 || x.Number == 2627);
        }

        public static bool IsUniqueKeyException(this MySqlException ex)
        {
            return ex.Number == 1060 ||
                ex.Number == 1061 ||
                ex.Number == 1062 ||
                ex.Number == 1088 ||
                ex.Number == 1092;
        }
    }
}
