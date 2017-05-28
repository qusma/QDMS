// -----------------------------------------------------------------------
// <copyright file="Instrument.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text;
using ProtoBuf;

namespace QDMS
{
    [ProtoContract]
    public class Instrument : ICloneable, IEquatable<Instrument>
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(20)]
        public int? ID { get; set; }

        [ProtoMember(1)]
        [MaxLength(100)]
        public string Symbol { get; set; }

        [ProtoMember(2)]
        [MaxLength(255)]
        public string UnderlyingSymbol { get; set; }

        [ProtoMember(89)]
        [MaxLength(255)]
        public string Name { get; set; }

        [ProtoMember(3)]
        public int? PrimaryExchangeID { get; set; }

        [ProtoMember(4)]
        public int? ExchangeID { get; set; }

        [ProtoMember(5)]
        public InstrumentType Type { get; set; }

        [ProtoMember(6)]
        public int? Multiplier { get; set; }

        public DateTime? Expiration
        {
            get
            {
                if (_expirationYear == 0 || _expirationMonth == 0 || _expirationDay == 0)
                    return null;
                else
                    return new DateTime(_expirationYear, _expirationMonth, _expirationDay);
            }

            set
            {
                if (value.HasValue)
                {
                    _expirationYear = value.Value.Year;
                    _expirationMonth = value.Value.Month;
                    _expirationDay = value.Value.Day;
                }
                else
                {
                    _expirationYear = 0;
                    _expirationMonth = 0;
                    _expirationDay = 0;
                }
            }
        }

        [ProtoMember(7)]
        [NotMapped]
        [NonSerialized]
        private int _expirationYear;

        [ProtoMember(8)]
        [NotMapped]
        [NonSerialized]
        private int _expirationMonth;

        [ProtoMember(9)]
        [NotMapped]
        [NonSerialized]
        private int _expirationDay;

        [ProtoMember(10)]
        public OptionType? OptionType { get; set; }

        [ProtoMember(11)]
        
        public decimal? Strike { get; set; }

        [ProtoMember(12)]
        [MaxLength(25)]
        public string Currency { get; set; }

        [ProtoMember(13)]
        public decimal? MinTick { get; set; }

        [ProtoMember(14)]
        [MaxLength(255)]
        public string Industry { get; set; }

        [ProtoMember(15)]
        [MaxLength(255)]
        public string Category { get; set; }

        [ProtoMember(16)]
        [MaxLength(255)]
        public string Subcategory { get; set; }

        [ProtoMember(17)]
        public bool IsContinuousFuture { get; set; }

        [ProtoMember(18)]
        public string ValidExchanges { get; set; }

        [ProtoMember(19)]
        public virtual ICollection<Tag> Tags { get; set; }

        [ProtoMember(90)]
        public int? DatasourceID { get; set; }

        [ProtoMember(21)]
        public virtual Exchange Exchange { get; set; }

        [ProtoMember(22)]
        public virtual Exchange PrimaryExchange { get; set; }

        [ProtoMember(23)]
        public virtual Datasource Datasource { get; set; }

        [ProtoMember(24)]
        public int? ContinuousFutureID { get; set; }

        public SessionsSource SessionsSource { get; set; }

        [ProtoMember(26)]
        public virtual ICollection<InstrumentSession> Sessions { get; set; }

        [ProtoMember(27)]
        public int? SessionTemplateID { get; set; }

        [ProtoMember(28)]
        [MaxLength(255)]
        public string DatasourceSymbol { get; set; }

        [ProtoMember(29)]
        public ContinuousFuture ContinuousFuture { get; set; }

        [ProtoMember(30)]
        [MaxLength(20)]
        public string TradingClass { get; set; }

        [NotMapped]
        public string TagsAsString
        {
            get
            {
                return (Tags == null || Tags.Count == 0) ? "" : string.Join(", ", Tags.Select(x => x.Name));
            }
        }

        /// <summary>
        /// Get timezone info for this instrument.
        /// </summary>
        /// <returns>Returns TimeZoneInfo for this instrument's exchange's timezone. If it's null, it returns UTC.</returns>
        public TimeZoneInfo GetTZInfo()
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                string.IsNullOrEmpty(Exchange?.Timezone)
                    ? "UTC" 
                    : Exchange.Timezone);
        }


        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Instrument other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.ID.Equals(ID);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("ID: " + ID);

            if (!string.IsNullOrEmpty(Symbol))
                sb.Append(" Symbol: " + Symbol);

            if (!string.IsNullOrEmpty(UnderlyingSymbol))
                sb.Append(" Underlying: " + UnderlyingSymbol);

            sb.Append(" Type: " + Type);

            if (OptionType.HasValue)
                sb.Append(string.Format(" ({0})", OptionType));

            if (Strike.HasValue && Strike != 0)
                sb.Append(" Strike: " + Strike);

            if (Expiration.HasValue)
                sb.Append(" Exp: " + Expiration.Value.ToString("dd-MM-yyyy"));

            if (IsContinuousFuture)
                sb.Append(" (CF)");

            if (Exchange != null)
                sb.Append(" Exch: " + Exchange.Name);

            if (Datasource != null)
                sb.Append(" DS: " + Datasource.Name);


            if(!string.IsNullOrEmpty(Currency))
                sb.Append(string.Format("({0})", Currency));

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new Instrument
            {
                ID = ID,
                Symbol = Symbol,
                UnderlyingSymbol = UnderlyingSymbol,
                PrimaryExchangeID = PrimaryExchangeID,
                Name = Name,
                ExchangeID = ExchangeID,
                Type = Type,
                Multiplier = Multiplier,
                Expiration = Expiration,
                OptionType = OptionType,
                Strike = Strike,
                Currency = Currency,
                MinTick = MinTick,
                Industry = Industry,
                Category = Category,
                Subcategory = Subcategory,
                IsContinuousFuture = IsContinuousFuture,
                ValidExchanges = ValidExchanges,
                DatasourceID = DatasourceID,
                Exchange = Exchange,
                PrimaryExchange = PrimaryExchange,
                Datasource = Datasource,
                Tags = Tags == null ? null : Tags.ToList(),
                Sessions = Sessions == null ? null : Sessions.Select(x => (InstrumentSession)x.Clone()).ToList(),
                SessionsSource = SessionsSource,
                SessionTemplateID = SessionTemplateID,
                DatasourceSymbol = DatasourceSymbol,
                TradingClass = TradingClass
            };

            if (ContinuousFuture != null)
            {
                clone.ContinuousFuture = (ContinuousFuture)ContinuousFuture.Clone();
            }
            return clone;
        }
    }
}
