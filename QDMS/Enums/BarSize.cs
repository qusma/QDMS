// -----------------------------------------------------------------------
// <copyright file="BarSize.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace QDMS
{
    /// <summary>
    /// Historical Bar Size Requests
    /// </summary>
    [Serializable]
   // [DataContract]
    //[ProtoContract]
    public enum BarSize : int
    {
        /// <summary>
        /// Tick bars
        /// </summary>
        //[ProtoEnum(Name="Tick", Value=0)]
        //[EnumMember]
        [Description("Tick")]
        Tick = 0,
        /// <summary>
        /// 1 second bars
        /// </summary>
        [Description("1 secs")]
        OneSecond = 1,
        /// <summary>
        /// 5 second bars
        /// </summary>
        [Description("5 secs")]
        FiveSeconds = 2,
        /// <summary>
        /// 15 second bars
        /// </summary>
        [Description("15 secs")]
        FifteenSeconds = 3,
        /// <summary>
        /// 30 second bars
        /// </summary>
        [Description("30 secs")]
        ThirtySeconds = 4,
        /// <summary>
        /// 1 minute bars
        /// </summary>
        [Description("1 min")]
        OneMinute = 5,
        /// <summary>
        /// 2 minute bars
        /// </summary>
        [Description("2 mins")]
        TwoMinutes = 6,
        /// <summary>
        /// 5 minute bars
        /// </summary>
        [Description("5 mins")]
        FiveMinutes = 7,
        /// <summary>
        /// 15 minute bars
        /// </summary>
        [Description("15 mins")]
        FifteenMinutes = 8,
        /// <summary>
        /// 30 minute bars
        /// </summary>
        [Description("30 mins")]
        ThirtyMinutes = 9,
        /// <summary>
        /// 1 hour bars
        /// </summary>
        [Description("1 hour")]
        OneHour = 10,
        /// <summary>
        /// 1 day bars
        /// </summary>
        [Description("1 day")]
        OneDay = 11,
        /// <summary>
        /// 1 week bars
        /// </summary>
        [Description("1 week")]
        OneWeek = 12,
        /// <summary>
        /// 1 month bars
        /// </summary>
        [Description("1 month")]
        OneMonth = 13,
        /// <summary>
        /// 1 year bars
        /// </summary>
        [Description("1 year")]
        OneYear = 14
    }
}
