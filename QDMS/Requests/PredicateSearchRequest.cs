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
    public class PredicateSearchRequest
    {
        private string _serializedFilter;

        /// <summary>
        /// For serialization use
        /// </summary>
        public PredicateSearchRequest() { }

        public PredicateSearchRequest(Expression<Func<Instrument, bool>> filter)
        {
            Filter = filter;
        }

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