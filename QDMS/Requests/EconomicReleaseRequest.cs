// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseRequest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Newtonsoft.Json;
using System;
using System.Linq.Expressions;

namespace QDMS
{
    /// <summary>
    /// Request for economic release data
    /// </summary>
    public class EconomicReleaseRequest
    {
        private string _serializedFilter;

        /// <summary>
        /// Request data for a single day
        /// </summary>
        /// <param name="date">The date, in UTC</param>
        /// <param name="dataLocation"></param>
        /// <param name="filter">Specify a filter. Only releases matching this filter will be returned.</param>
        /// <param name="dataSource">Specify a specific datasource. Otherwise the default one will be used.</param>
        public EconomicReleaseRequest(DateTime date, DataLocation dataLocation = DataLocation.ExternalOnly, Expression<Func<EconomicRelease, bool>> filter = null, string dataSource = null)
            : this(date, date, dataLocation, filter, dataSource)
        {
        }

        /// <summary>
        /// Request data for a period
        /// </summary>
        /// <param name="fromDate">The start of the period, in UTC</param>
        /// <param name="toDate">The end of the period, in UTC</param>
        /// <param name="dataLocation"></param>
        /// <param name="filter">Specify a filter. Only releases matching this filter will be returned.</param>
        /// <param name="dataSource">Specify a specific datasource. Otherwise the default one will be used.</param>
        /// <exception cref="ArgumentException">fromDate must be before toDate</exception>
        public EconomicReleaseRequest(DateTime fromDate, DateTime toDate, DataLocation dataLocation = DataLocation.ExternalOnly, Expression<Func<EconomicRelease, bool>> filter = null, string dataSource = null)
        {
            if (fromDate.Date > toDate.Date) throw new ArgumentException("fromDate must be before toDate");

            FromDate = fromDate;
            ToDate = toDate;
            DataLocation = dataLocation;
            Filter = filter;
            DataSource = dataSource;
        }

        /// <summary>
        /// 
        /// </summary>
        [Obsolete("FOR SERIALIZATION USE ONLY")]
        public EconomicReleaseRequest()
        { }

        /// <summary>
        /// 
        /// </summary>
        public DataLocation DataLocation { get; set; }

        /// <summary>
        /// If this is not specified, the default datasource will be used.
        /// </summary>
        public string DataSource { get; }

        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public Expression<Func<EconomicRelease, bool>> Filter
        {
            get
            {
                return ExpressionSerializer.Deserialize<EconomicRelease>(_serializedFilter);
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

        /// <summary>
        /// 
        /// </summary>
        public DateTime FromDate { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime ToDate { get; set; }
    }
}