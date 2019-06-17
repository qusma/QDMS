// -----------------------------------------------------------------------
// <copyright file="PredicateSearchRequest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Linq.Expressions;

namespace QDMS
{
    /// <summary>
    /// For internal use. Used to send predicate filter requests
    /// </summary>
    public class PredicateSearchRequest
    {
        private string _serializedFilter;

        /// <summary>
        /// For serialization use
        /// </summary>
        public PredicateSearchRequest() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        public PredicateSearchRequest(Expression<Func<Instrument, bool>> filter)
        {
            Filter = filter;
        }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public Expression<Func<Instrument, bool>> Filter
        {
            get
            {
                return ExpressionSerializer.Deserialize<Instrument>(_serializedFilter);
            }
            set
            {
                if (value == null)
                {
                    _serializedFilter = null;
                    return;
                }

                _serializedFilter = value.Serialize();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("FOR SERIALIZATION USE ONLY")]
        public string SerializedFilter
        {
            get
            {
                return _serializedFilter;
            }
            set
            {
                _serializedFilter = value;
            }
        }
    }
}