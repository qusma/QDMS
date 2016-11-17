// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace QDMSClient
{
    internal static class ExtensionMethods
    {
        private static readonly ConcurrentDictionary<Type, List<PropertyInfo>> PropertyInfoCache = new ConcurrentDictionary<Type, List<PropertyInfo>>();

        /// <summary>
        /// Returns a dictionary in which the keys are the names of each public property and the values the serialized values.
        /// Ignores nested objects.
        /// Ignore properties with the JsonIgnore attribute.
        /// Used to construct URL query strings
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null" />.</exception>
        internal static Dictionary<string, string> GetSerializedPropertyValues(this object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var type = obj.GetType();
            if (!PropertyInfoCache.ContainsKey(type))
            {
                var properties = type
                    .GetProperties()
                    .Where(x => x.GetCustomAttribute(typeof(JsonIgnoreAttribute), true) == null)
                    .Where(x => x.PropertyType.IsClass == false || x.PropertyType == typeof(string)) //can't serialize nested objects
                    .ToList();
                PropertyInfoCache.TryAdd(type, properties);
            }

            //use JSON.Net to serialize everything to a string
            return PropertyInfoCache[type]
                .Select(x => new { x.Name, Value = x.GetValue(obj) })
                .Where(x => x.Value != null)
                .ToDictionary(
                    x => x.Name,
                    x => x.Value is string ? (string)x.Value : JsonConvert.SerializeObject(x.Value).Trim('"'));
        }
    }
}