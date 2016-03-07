// -----------------------------------------------------------------------
// <copyright file="MyUtils.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProtoBuf;
using QLNet;

namespace QDMS
{
    public static class MyUtils
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Returns a string with the ordinal suffix of a number, i.e. 1 -> "1st"
        /// </summary>
        public static string Ordinal(int num)
        {
            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";

                case 2:
                    return num + "nd";

                case 3:
                    return num + "rd";

                default:
                    return num + "th";
            }
        }

        /// <summary>
        /// Given the root symbol, year, and month, returns a string of the future contract symbol
        /// based on the US letter-based month system.
        /// </summary>
        public static string GetFuturesContractSymbol(string baseSymbol, int month, int year)
        {
            return string.Format("{0}{1}{2}", baseSymbol, GetFuturesMonthSymbol(month), year % 10);
        }

        private static string GetFuturesMonthSymbol(int month)
        {
            switch (month)
            {
                case 1:
                    return "F";

                case 2:
                    return "G";

                case 3:
                    return "H";

                case 4:
                    return "J";

                case 5:
                    return "K";

                case 6:
                    return "M";

                case 7:
                    return "N";

                case 8:
                    return "Q";

                case 9:
                    return "U";

                case 10:
                    return "V";

                case 11:
                    return "X";

                case 12:
                    return "Z";
            }
            throw new ArgumentOutOfRangeException("month", "Month must be between 1-12");
        }

        /// <summary>
        /// Gets a calendar from a 2-letter country code.
        /// </summary>
        public static Calendar GetCalendarFromCountryCode(string country)
        {
            switch (country)
            {
                case "CH":
                    return new Switzerland();

                case "US":
                    return new UnitedStates(UnitedStates.Market.NYSE);

                case "SG":
                    return new Singapore();

                case "UK":
                    return new UnitedKingdom(UnitedKingdom.Market.Exchange);

                case "DE":
                    return new Germany(Germany.Market.FrankfurtStockExchange);

                case "HK":
                    return new HongKong();

                case "JP":
                    return new Japan();

                case "SK":
                    return new SouthKorea(SouthKorea.Market.KRX);

                case "BR":
                    return new Brazil(Brazil.Market.Exchange);

                case "AU":
                    return new Australia();

                case "IN":
                    return new India();

                case "CN":
                    return new China();

                case "TW":
                    return new Taiwan();

                case "IT":
                    return new Italy(Italy.Market.Exchange);

                case "CA":
                    return new Canada(Canada.Market.TSX);

                case "ID":
                    return new Indonesia(Indonesia.Market.JSX);

                case "SE":
                    return new Sweden();
            }

            return new UnitedStates();
        }

        /// <summary>
        /// Converts a datetime to a UNIX epoch-based timestamp.
        /// </summary>
        public static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime TimestampToDateTime(long timestamp)
        {
            return Epoch.AddSeconds(timestamp);
        }

        public static DateTime TimestampToDateTimeByMillisecound(long timestamp)
        {
            return Epoch.AddMilliseconds(timestamp);
        }

        /// <summary>
        /// Returns an IEnumerable of all possible values of an Enum.
        /// </summary>
        public static IEnumerable<T> GetEnumValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Serialize object using protocol buffers.
        /// </summary>
        public static byte[] ProtoBufSerialize(object input, MemoryStream ms)
        {
            ms.SetLength(0);
            Serializer.Serialize(ms, input);
            ms.Position = 0;
            byte[] buffer = new byte[ms.Length];
            ms.Read(buffer, 0, (int)ms.Length);
            return buffer;
        }

        /// <summary>
        /// Deserialize object of type T using protocol buffers.
        /// </summary>
        public static T ProtoBufDeserialize<T>(byte[] input, MemoryStream ms)
        {
            ms.SetLength(0);
            ms.Write(input, 0, input.Length);
            ms.Position = 0;
            return Serializer.Deserialize<T>(ms);
        }

        /// <summary>
        /// Converts a BarSize to its corresponding timespan.
        /// </summary>
        public static TimeSpan ToTimeSpan(this BarSize size)
        {
            switch (size)
            {
                case BarSize.Tick:
                    return TimeSpan.FromTicks(1);

                case BarSize.OneSecond:
                    return TimeSpan.FromSeconds(1);

                case BarSize.FiveSeconds:
                    return TimeSpan.FromSeconds(5);

                case BarSize.FifteenSeconds:
                    return TimeSpan.FromSeconds(15);

                case BarSize.ThirtySeconds:
                    return TimeSpan.FromSeconds(30);

                case BarSize.OneMinute:
                    return TimeSpan.FromMinutes(1);

                case BarSize.TwoMinutes:
                    return TimeSpan.FromMinutes(2);

                case BarSize.FiveMinutes:
                    return TimeSpan.FromMinutes(5);

                case BarSize.FifteenMinutes:
                    return TimeSpan.FromMinutes(15);

                case BarSize.ThirtyMinutes:
                    return TimeSpan.FromMinutes(30);

                case BarSize.OneHour:
                    return TimeSpan.FromHours(1);

                case BarSize.OneDay:
                    return TimeSpan.FromDays(1);

                case BarSize.OneWeek:
                    return TimeSpan.FromDays(7);

                case BarSize.OneMonth:
                    return TimeSpan.FromDays(30);

                case BarSize.OneYear:
                    return TimeSpan.FromDays(365);

                default:
                    return TimeSpan.FromDays(1);
            }
        }

        /// <summary>
        /// Converts a timespan to its corresponding BarSize, if possible.
        /// </summary>
        public static BarSize ToBarSize(this TimeSpan span)
        {
            if (span <= TimeSpan.FromTicks(1))
                return BarSize.Tick;

            if (span <= TimeSpan.FromSeconds(1))
                return BarSize.OneSecond;

            if (span <= TimeSpan.FromSeconds(5))
                return BarSize.FiveSeconds;

            if (span <= TimeSpan.FromSeconds(15))
                return BarSize.FifteenSeconds;

            if (span <= TimeSpan.FromSeconds(30))
                return BarSize.ThirtySeconds;

            if (span <= TimeSpan.FromMinutes(1))
                return BarSize.OneMinute;

            if (span <= TimeSpan.FromMinutes(2))
                return BarSize.TwoMinutes;

            if (span <= TimeSpan.FromMinutes(5))
                return BarSize.FiveMinutes;

            if (span <= TimeSpan.FromMinutes(15))
                return BarSize.FifteenMinutes;

            if (span <= TimeSpan.FromMinutes(30))
                return BarSize.ThirtyMinutes;

            if (span <= TimeSpan.FromHours(1))
                return BarSize.OneHour;
            
            if (span <= TimeSpan.FromDays(1))
                return BarSize.OneDay;

            if (span <= TimeSpan.FromDays(7))
                return BarSize.OneWeek;

            if (span <= TimeSpan.FromDays(30))
                return BarSize.OneMonth;

            if (span <= TimeSpan.FromDays(365))
                return BarSize.OneYear;

            throw new ArgumentException("The timespan " + span + " is not supported for the function ToBarSize(this Timespan)");
        }

        /// <summary>
        /// Returns the unmber of business days between two dates, not including the final day.
        /// </summary>
        public static int BusinessDaysBetween(DateTime start, DateTime end, Calendar cal)
        {
            if (start > end) throw new Exception("Ending date must be later than starting date");
            if (start == end) return 0;
            int count = 0;
            while (start < end)
            {
                if (cal.isBusinessDay(start))
                    count++;
                start = start.AddDays(1);
            }

            return count;
        }

        /// <summary>
        /// Sets bar open and closing times to the opening and closing of the day's sessions
        /// </summary>
        /// <param name="bars"></param>
        /// <param name="instrument"></param>
        public static void SetSessionTimes(IEnumerable<OHLCBar> bars, Instrument instrument)
        {
            Dictionary<int, InstrumentSession> openingSessions = instrument.SessionStartTimesByDay();
            Dictionary<int, TimeSpan> closingSessions = instrument.SessionEndTimesByDay();

            foreach (OHLCBar bar in bars)
            {
                int dotw = bar.DT.DayOfWeek.ToInt();

                //set opening time
                if (openingSessions.ContainsKey(dotw))
                {
                    if ((int)openingSessions[dotw].OpeningDay != dotw)
                    {
                        //the opening is on a different day, move back
                        int daysToMoveBack =
                            (int)openingSessions[dotw].OpeningDay <= dotw
                            ? dotw - (int)openingSessions[dotw].OpeningDay
                            : 7 - ((int)openingSessions[dotw].OpeningDay - dotw);
                        bar.DTOpen = new DateTime(bar.DT.Year, bar.DT.Month, bar.DT.Day).AddDays(-daysToMoveBack) + openingSessions[dotw].OpeningTime;
                        //TODO write a test for this stuff
                    }
                    else
                    {
                        bar.DTOpen = new DateTime(bar.DT.Year, bar.DT.Month, bar.DT.Day) + openingSessions[dotw].OpeningTime;
                    }
                }
                else
                {
                    bar.DTOpen = new DateTime(bar.DT.Year, bar.DT.Month, bar.DT.Day, 0, 0, 0);
                }

                //set closing time
                if (closingSessions.ContainsKey(dotw))
                {
                    bar.DT = new DateTime(bar.DT.Year, bar.DT.Month, bar.DT.Day) + closingSessions[dotw];
                }
                else
                {
                    bar.DT = new DateTime(bar.DT.Year, bar.DT.Month, bar.DT.Day, 23, 59, 59);
                }
            }
        }

        /// <summary>
        /// Ensure that no sessions in the collection overlap.
        /// </summary>
        public static void ValidateSessions(List<ISession> sessions)
        {
            sessions = sessions.OrderBy(x => x.ClosingDay).ThenBy(x => x.ClosingTime).ToList();
            //first test last vs first, then in a row
            if (sessions.First().Overlaps(sessions.Last()))
                throw new Exception(string.Format("Sessions overlap: {0} and {1}", sessions.First(), sessions.Last()));
            for (int i = 0; i < sessions.Count - 1; i++)
            {
                if (sessions[i].Overlaps(sessions[i + 1]))
                    throw new Exception(string.Format("Sessions overlap: {0} and {1}", sessions.First(), sessions.Last()));
            }
        }
    }
}