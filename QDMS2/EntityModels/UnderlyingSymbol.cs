﻿// -----------------------------------------------------------------------
// <copyright file="UnderlyingSymbol.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

//In the future this class can hold stuff like margin requirements as well.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using Newtonsoft.Json;
using ProtoBuf;
using QLNet;

namespace QDMS
{
    /// <summary>
    /// Represents a futures contract specification, including the root symbol and expiration date rules
    /// </summary>
    [ProtoContract]
    public class UnderlyingSymbol : IEntity, IEquatable<UnderlyingSymbol>
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProtoMember(2)]
        [MaxLength(255)]
        public string Symbol { get; set; }

        //The byte is what we save to the database, the ExpirationRule is what we use in our applications
        /// <summary>
        /// 
        /// </summary>
        [JsonIgnore]
        public byte[] ExpirationRule
        {
            get => MyUtils.ProtoBufSerialize(Rule, new MemoryStream());
            set => Rule = MyUtils.ProtoBufDeserialize<ExpirationRule>(value, new MemoryStream());
        }

        /// <summary>
        /// The rules that determine the expiration date for future contracts of this class
        /// </summary>
        [NotMapped]
        [ProtoMember(3)]
        public ExpirationRule Rule { get; set; }

        /// <summary>
        /// Returns the expiration date for a future expiring in a particular year/month.
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="countryCode"></param>
        /// <returns></returns>
        public DateTime ExpirationDate(int year, int month, string countryCode = "US")
        {
            DateTime referenceDay = new DateTime(year, month, 1);
            referenceDay = referenceDay.AddMonths((int)Rule.ReferenceRelativeMonth);

            Calendar calendar = MyUtils.GetCalendarFromCountryCode(countryCode);

            int day = 0;
            if (Rule.ReferenceDayType == ReferenceDayType.LastDayOfMonth)
            {
                var tmpDay = referenceDay.AddMonths(1);
                tmpDay = tmpDay.AddDays(-1);
                while (!calendar.isBusinessDay(tmpDay))
                {
                    tmpDay = tmpDay.AddDays(-1);
                }
                day = tmpDay.Day;
            }
            else if (Rule.ReferenceDayType == ReferenceDayType.CalendarDays) //we use a fixed number of days from the start of the month
            {
                day = Rule.ReferenceDays;
            }
            else if (Rule.ReferenceDayType == ReferenceDayType.WeekDays) //we use a number of weeks and then a weekday of that week
            {
                if (Rule.ReferenceWeekDayCount == WeekDayCount.Last) //the last week of the month
                {
                    var tmpDay = referenceDay.AddMonths(1);
                    tmpDay = tmpDay.AddDays(-1);
                    while (tmpDay.DayOfWeek.ToInt() != (int)Rule.ReferenceWeekDay)
                    {
                        tmpDay = tmpDay.AddDays(-1);
                    }
                    day = tmpDay.Day;
                }
                else //1st to 4th week of the month, just loop until we find the right day
                {
                    int weekCount = 0;
                    while (weekCount < (int)Rule.ReferenceWeekDayCount + 1)
                    {
                        if (referenceDay.DayOfWeek.ToInt() == (int)Rule.ReferenceWeekDay)
                            weekCount++;

                        referenceDay = referenceDay.AddDays(1);
                    }

                    day = referenceDay.Day - 1;
                }
            }

            referenceDay = new DateTime(year, month, day);
            referenceDay = referenceDay.AddMonths((int)Rule.ReferenceRelativeMonth);

            if (Rule.ReferenceDayMustBeBusinessDay)
            {
                while (!calendar.isBusinessDay(referenceDay))
                {
                    referenceDay = referenceDay.AddDays(-1);
                }
            }

            if (Rule.DayType == DayType.Business)
            {
                int daysLeft = Rule.DaysBefore;
                int daysBack = 0;
                while (daysLeft > 0)
                {
                    daysBack++;

                    if (calendar.isBusinessDay(referenceDay.AddDays(-daysBack)))
                        daysLeft--;
                }
                return referenceDay.AddDays(-daysBack);
            }
            else if (Rule.DayType == DayType.Calendar)
            {
                return referenceDay.AddDays(-Rule.DaysBefore);
            }
            return referenceDay;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public object Clone()
        {
            var clone = new UnderlyingSymbol
            {
                ID = ID,
                ExpirationRule = ExpirationRule,
                Symbol = Symbol
            };

            return clone;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Symbol;
        }

        /// <inheritdoc />
        public bool Equals(UnderlyingSymbol other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ID == other.ID && string.Equals(Symbol, other.Symbol);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnderlyingSymbol)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = ID;
                hashCode = (hashCode * 397) ^ (Symbol != null ? Symbol.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}