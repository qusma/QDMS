// -----------------------------------------------------------------------
// <copyright file="GenericExtensions.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QDMS
{
    /// <summary>
    /// Used internally
    /// </summary>
    public static class GenericExtensions
    {
        /// <summary>
        /// Returns distinct values in an IEnumerable based on a lamdba expression that tests for equality between elements.
        /// </summary>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> values, Func<T, T, bool> lamdbaExpression)
        {
            return values.Distinct(new LambdaEqualityComparer<T>(lamdbaExpression));
        }

        /// <summary>
        /// Returns distinct values in an IEnumerable based on a lamdba expression that generates object hashes.
        /// </summary>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> values, Func<T, int> lamdbaExpression)
        {
            return values.Distinct(new LambdaEqualityComparer<T>(lamdbaExpression));
        }

        /// <summary>
        /// Returns the element that occurs most frequently in this collection.
        /// </summary>
        public static T MostFrequent<T>(this IEnumerable<T> data)
        {
            if (data.Count() == 0) throw new Exception("Data must contain at least one element");
            if (data.Count() == 1) return data.First();

            var occurences = new Dictionary<T, int>();
            foreach (T item in data)
            {
                if (occurences.ContainsKey(item))
                {
                    occurences[item]++;
                }
                else
                {
                    occurences.Add(item, 1);
                }
            }

            int maxOccurences = occurences.Values.Max();
            return occurences.Where(x => x.Value == maxOccurences).Select(x => x.Key).First();
        }

        /// <summary>
        /// Finds the index of the first occurence of an item matching the provided predicate
        /// </summary>
        /// <returns>The index if found, -1 if not found.</returns>
        public static int IndexOf<T>(this IEnumerable<T> data, Predicate<T> predicate)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (predicate == null) throw new ArgumentNullException("predicate");

            int count = 0;
            foreach (var i in data)
            {
                if (predicate(i)) return count;
                count++;
            }
            return -1;
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        public static void RemoveAll<T>(this ObservableCollection<T> collection, Func<T, bool> predicate)
        {
            var toRemove = collection.Where(predicate).ToList();
            foreach (T item in toRemove)
                collection.Remove(item);
        }
    }
}
