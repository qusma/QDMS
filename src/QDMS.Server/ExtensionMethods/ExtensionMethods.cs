// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace QDMS.Server
{
    public static class ExtensionMethods
    {
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

        /// <summary>
        /// True if the exception (or one of its inner exceptions) is a Postgres unique constraint violation.
        /// </summary>
        public static bool IsUniqueKeyException(this Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation) return true;
            }

            return false;
        }

        /// <summary>
        /// Inserts the entity, or copies its values onto the existing one matching the predicate.
        /// Replaces EF6's AddOrUpdate. Checks entities tracked earlier in the same unit of work first.
        /// </summary>
        public static void Upsert<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> match, T entity) where T : class
        {
            var existing = dbSet.Local.FirstOrDefault(match.Compile()) ?? dbSet.FirstOrDefault(match);
            if (existing == null)
            {
                dbSet.Add(entity);
                return;
            }

            if (ReferenceEquals(existing, entity)) return;

            //keep the existing primary key, update everything else
            var entry = dbSet.Entry(existing);
            foreach (var keyProperty in entry.Metadata.FindPrimaryKey().Properties)
            {
                keyProperty.PropertyInfo.SetValue(entity, keyProperty.PropertyInfo.GetValue(existing));
            }

            entry.CurrentValues.SetValues(entity);
        }
    }
}
