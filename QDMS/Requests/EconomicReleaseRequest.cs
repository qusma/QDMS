// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseRequest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using MetaLinq;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Xml.Serialization;

namespace QDMS
{
    [ProtoContract]
    public class EconomicReleaseRequest
    {
        private string _serializedFilter;

        [Obsolete("FOR SERIALIZATION USE ONLY")]
        public EconomicReleaseRequest()
        { }

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

        [ProtoIgnore]
        [JsonIgnore]
        public Expression<Func<EconomicRelease, bool>> Filter
        {
            get
            {
                if (_serializedFilter == null) return null;

                var ms = new StringReader(_serializedFilter);

                var xs = new XmlSerializer(typeof(EditableExpression),
                    new[] { typeof(MetaLinq.Expressions.EditableLambdaExpression) });

                //Deserialize LINQ expression
                var editableExp = (EditableExpression)xs.Deserialize(ms);
                var expression = (Expression<Func<EconomicRelease, bool>>)editableExp.ToExpression();
                return expression;
            }
            set
            {
                if (value == null)
                {
                    _serializedFilter = null;
                    return;
                }

                using (StringWriter textWriter = new StringWriter())
                {
                    EditableExpression mutable = EditableExpression.CreateEditableExpression(value);
                    XmlSerializer xs = new XmlSerializer(typeof(EditableExpression),
                        new[] { typeof(MetaLinq.Expressions.EditableLambdaExpression) });

                    xs.Serialize(textWriter, mutable);
                    _serializedFilter = textWriter.ToString();
                }
            }
        }

        [ProtoMember(4)]
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
        /// If this is not specified, the default datasource will be used.
        /// </summary>
        [ProtoMember(1)]
        public string DataSource { get; }

        [ProtoMember(2)]
        public DateTime FromDate { get; }

        [ProtoMember(3)]
        public DateTime ToDate { get; }

        [ProtoMember(5)]
        public DataLocation DataLocation { get; }
    }
}