// -----------------------------------------------------------------------
// <copyright file="MyUtils.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
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

        public static string GetFuturesContractSymbol(string baseSymbol, int month, int year)
        {
            return string.Format("{0}{1}{2}", baseSymbol, GetFuturesMonthSymbol(month), year % 10);
        }

        public static string GetFuturesMonthSymbol(int month)
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
            return "";
        }

        /// <summary>
        /// Gets a calendar from a 2-letter country code.
        /// </summary>
        public static Calendar GetCalendarFromCountryCode(string country)
        {
            if (country == "CH")
            {
                return new Switzerland();
            }
            else if (country == "US")
            {
                return new UnitedStates(UnitedStates.Market.NYSE);
            }
            else if (country == "SG")
            {
                return new Singapore();
            }
            else if (country == "UK")
            {
                return new UnitedKingdom(UnitedKingdom.Market.Exchange);
            }
            else if (country == "DE")
            {
                return new Germany(Germany.Market.FrankfurtStockExchange);
            }
            else if (country == "HK")
            {
                return new HongKong();
            }
            else if (country == "JP")
            {
                return new Japan();
            }
            else if (country == "SK")
            {
                return new SouthKorea(SouthKorea.Market.KRX);
            }
            else if (country == "BR")
            {
                return new Brazil(Brazil.Market.Exchange);
            }
            else if (country == "AU")
            {
                return new Australia();
            }
            else if (country == "IN")
            {
                return new India();
            }
            else if (country == "CN")
            {
                return new China();
            }
            else if (country == "TW")
            {
                return new Taiwan();
            }
            else if (country == "IT")
            {
                return new Italy(Italy.Market.Exchange);
            }
            else if (country == "CA")
            {
                return new Canada(Canada.Market.TSX);
            }
            else if (country == "ID")
            {
                return new Indonesia(Indonesia.Market.JSX);
            }
            else if (country == "SE")
            {
                return new Sweden();
            }

            return new UnitedStates();
        }

        public static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long) elapsedTime.TotalSeconds;
        }

        public static int ToInt(this DayOfWeek value)
        {
            switch (value)
            {
                case DayOfWeek.Sunday:
                    return 6;
                    
                case DayOfWeek.Monday:
                    return 0;
                    
                case DayOfWeek.Tuesday:
                    return 1;
                    
                case DayOfWeek.Wednesday:
                    return 2;
                    
                case DayOfWeek.Thursday:
                    return 3;
                    
                case DayOfWeek.Friday:
                    return 4;
                    
                case DayOfWeek.Saturday:
                    return 5;
                    
            }
            return 0;
        }

        public static IEnumerable<T> GetEnumValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static byte[] ProtoBufSerialize(object input, MemoryStream ms)
        {
            ms.SetLength(0);
            Serializer.Serialize(ms, input);
            ms.Position = 0;
            byte[] buffer = new byte[ms.Length];
            ms.Read(buffer, 0, (int)ms.Length);
            return buffer;
        }

        public static T ProtoBufDeserialize<T>(byte[] input, MemoryStream ms)
        {
            ms.SetLength(0);
            ms.Write(input, 0, input.Length);
            ms.Position = 0;
            return Serializer.Deserialize<T>(ms);
        }

        public static InstrumentSession SessionConverter(ExchangeSession session)
        {
            var result = new InstrumentSession();
            result.OpeningDay = session.OpeningDay;
            result.OpeningTime = TimeSpan.FromSeconds(session.OpeningTime.TotalSeconds);
            result.ClosingDay = session.ClosingDay;
            result.ClosingTime = TimeSpan.FromSeconds(session.ClosingTime.TotalSeconds);
            result.IsSessionEnd = session.IsSessionEnd;
            return result;
        }

        public static InstrumentSession SessionConverter(TemplateSession session)
        {
            var result = new InstrumentSession();
            result.OpeningDay = session.OpeningDay;
            result.OpeningTime = TimeSpan.FromSeconds(session.OpeningTime.TotalSeconds);
            result.ClosingDay = session.ClosingDay;
            result.ClosingTime = TimeSpan.FromSeconds(session.ClosingTime.TotalSeconds);
            result.IsSessionEnd = session.IsSessionEnd;
            return result;
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
    }
}
