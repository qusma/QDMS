// -----------------------------------------------------------------------
// <copyright file="ContinuousFuture.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProtoBuf;

namespace QDMS
{
    /// <summary>
    /// Represents a composite instrument, which strings together futures of different maturities to create a continuous one
    /// </summary>
    [ProtoContract]
    public class ContinuousFuture : ICloneable
    {
        /// <summary>
        /// 
        /// </summary>
        public ContinuousFuture()
        {
            UseJan = true;
            UseFeb = true;
            UseMar = true;
            UseApr = true;
            UseMay = true;
            UseJun = true;
            UseJul = true;
            UseAug = true;
            UseSep = true;
            UseOct = true;
            UseNov = true;
            UseDec = true;

            Month = 1;
        }

        /// <summary>
        /// Continuous future ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        /// <summary>
        /// Instrument ID
        /// </summary>
        [ProtoMember(2)]
        public int InstrumentID { get; set; }

        /// <summary>
        /// Instrument
        /// </summary>
        public virtual Instrument Instrument { get; set; }

        /// <summary>
        /// Underlying symbol ID
        /// </summary>
        [ProtoMember(3)]
        public int UnderlyingSymbolID { get; set; }

        /// <summary>
        /// The underlying symbol that the continuous future is based on.
        /// </summary>
        [ProtoMember(20)]
        public virtual UnderlyingSymbol UnderlyingSymbol { get; set; }

        /// <summary>
        /// Which contract month to use to construct the continuous prices.
        /// For example, Month = 1 uses the "front" future, Month = 2 uses the next one and so forth.
        /// </summary>
        [ProtoMember(4)]
        public int Month { get; set; }

        /// <summary>
        /// What criteria should be used when determining whether to roll over to the next contract.
        /// </summary>
        [ProtoMember(5)]
        public ContinuousFuturesRolloverType RolloverType { get; set; }

        /// <summary>
        /// Number of days that the criteria will use to determine rollover.
        /// </summary>
        [ProtoMember(6)]
        public int RolloverDays { get; set; }

        /// <summary>
        /// How to adjust prices from one contract to the next
        /// </summary>
        [ProtoMember(7)]
        public ContinuousFuturesAdjustmentMode AdjustmentMode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(8, IsRequired = true)]
        public bool UseJan { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(9, IsRequired = true)]
        public bool UseFeb { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(10, IsRequired = true)]
        public bool UseMar { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(11, IsRequired = true)]
        public bool UseApr { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(12, IsRequired = true)]
        public bool UseMay { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(13, IsRequired = true)]
        public bool UseJun { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(14, IsRequired = true)]
        public bool UseJul { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(15, IsRequired = true)]
        public bool UseAug { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(16, IsRequired = true)]
        public bool UseSep { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(17, IsRequired = true)]
        public bool UseOct { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(18, IsRequired = true)]
        public bool UseNov { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(19, IsRequired = true)]
        public bool UseDec { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        public bool MonthIsUsed(int month)
        {
            switch (month)
            {
                case 1:
                    return UseJan;
                case 2:
                    return UseFeb;
                case 3:
                    return UseMar;
                case 4:
                    return UseApr;
                case 5:
                    return UseMay;
                case 6:
                    return UseJun;
                case 7:
                    return UseJul;
                case 8:
                    return UseAug;
                case 9:
                    return UseSep;
                case 10:
                    return UseOct;
                case 11:
                    return UseNov;
                case 12:
                    return UseDec;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new ContinuousFuture
            {
                ID = ID,
                InstrumentID = InstrumentID,
                Instrument = Instrument,
                UnderlyingSymbol = UnderlyingSymbol,
                UnderlyingSymbolID = UnderlyingSymbolID,
                Month = Month,
                RolloverType = RolloverType,
                RolloverDays = RolloverDays,
                AdjustmentMode = AdjustmentMode,
                UseJan = UseJan,
                UseFeb = UseFeb,
                UseMar = UseMar,
                UseApr = UseApr,
                UseMay = UseMay,
                UseJun = UseJun,
                UseJul = UseJul,
                UseAug = UseAug,
                UseSep = UseSep,
                UseOct = UseOct,
                UseNov = UseNov,
                UseDec = UseDec
            };

            return clone;
        }
    }
}
