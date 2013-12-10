// -----------------------------------------------------------------------
// <copyright file="UnderlyingSymbol.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProtoBuf;
using QLNet;

namespace QDMS
{
    [ProtoContract]
    [Serializable]
    public class UnderlyingSymbol
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [ProtoMember(1)]
        public int ID { get; set; }

        [ProtoMember(2)]
        public string Symbol { get; set; }

        //The byte is what we save to the database, the ExpirationRule is what we use in our applications
        public byte[] ExpirationRule
        {
            get
            {
                return MyUtils.ProtoBufSerialize(Rule, new System.IO.MemoryStream());
            }
            set
            {
                Rule = MyUtils.ProtoBufDeserialize<ExpirationRule>(value, new System.IO.MemoryStream());
            }
        }

        [NotMapped]
        [ProtoMember(3)]
        public ExpirationRule Rule { get; set; }

        public DateTime ExpirationDate(int year, int month, string calendarType = "US")
        {
            DateTime referenceDay = new DateTime(year, month, 1);
            referenceDay = referenceDay.AddMonths((int)Rule.ReferenceRelativeMonth);

            int day;
            if (Rule.ReferenceUsesDays) //we use a fixed number of days from the start of the month
            {
                day = Rule.ReferenceDays;
            }
            else //we use a number of weeks and then a weekday of that week
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

            if (Rule.DayType == DayType.BusinessDay)
            {
                Calendar calendar = new UnitedStates(UnitedStates.Market.NYSE); //todo add functionality to allow changing this to other countries
                int daysLeft = Rule.DaysBefore;
                int daysBack = 0;
                while (daysLeft > 0)
                {
                    if (calendar.isBusinessDay(referenceDay.AddDays(-daysBack))) //todo fix here...
                        daysLeft--;

                    daysBack++;
                }
                return referenceDay.AddDays(-daysBack);
            }
            else if (Rule.DayType == DayType.CalendarDay)
            {
                return referenceDay.AddDays(-Rule.DaysBefore);
            }
            return referenceDay;
        }

    }
}