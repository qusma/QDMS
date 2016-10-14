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

        private static readonly Dictionary<string, string> CountryCodes = new Dictionary<string, string>
        {
            { "Afghanistan", "AF" },
            { "Albania", "AL" },
            { "Algeria", "DZ" },
            { "American Samoa", "AS" },
            { "Andorra", "AD" },
            { "Angola", "AO" },
            { "Anguilla", "AI" },
            { "Antarctica", "AQ" },
            { "Antigua & Barbuda", "AG" },
            { "Argentina", "AR" },
            { "Armenia", "AM" },
            { "Aruba", "AW" },
            { "Australia", "AU" },
            { "Austria", "AT" },
            { "Azerbaijan", "AZ" },
            { "Bahamas", "BS" },
            { "Bahrain", "BH" },
            { "Bangladesh", "BD" },
            { "Barbados", "BB" },
            { "Belarus", "BY" },
            { "Belgium", "BE" },
            { "Belize", "BZ" },
            { "Benin", "BJ" },
            { "Bermuda", "BM" },
            { "Bhutan", "BT" },
            { "Bolivia", "BO" },
            { "Bosnia", "BA" },
            { "Botswana", "BW" },
            { "Bouvet Island", "BV" },
            { "Brazil", "BR" },
            { "British Indian Ocean Territory", "IO" },
            { "British Virgin Islands", "VG" },
            { "Brunei", "BN" },
            { "Bulgaria", "BG" },
            { "Burkina Faso", "BF" },
            { "Burundi", "BI" },
            { "Cambodia", "KH" },
            { "Cameroon", "CM" },
            { "Canada", "CA" },
            { "Cape Verde", "CV" },
            { "Caribbean Netherlands", "BQ" },
            { "Cayman Islands", "KY" },
            { "Central African Republic", "CF" },
            { "Chad", "TD" },
            { "Chile", "CL" },
            { "China", "CN" },
            { "Christmas Island", "CX" },
            { "Cocos (Keeling) Islands", "CC" },
            { "Colombia", "CO" },
            { "Comoros", "KM" },
            { "Congo - Brazzaville", "CG" },
            { "Congo - Kinshasa", "CD" },
            { "Cook Islands", "CK" },
            { "Costa Rica", "CR" },
            { "Croatia", "HR" },
            { "Cuba", "CU" },
            { "Curaçao", "CW" },
            { "Cyprus", "CY" },
            { "Czech Republic", "CZ" },
            { "Côte d’Ivoire", "CI" },
            { "Denmark", "DK" },
            { "Djibouti", "DJ" },
            { "Dominica", "DM" },
            { "Dominican Republic", "DO" },
            { "Ecuador", "EC" },
            { "Egypt", "EG" },
            { "El Salvador", "SV" },
            { "Equatorial Guinea", "GQ" },
            { "Eritrea", "ER" },
            { "Estonia", "EE" },
            { "Ethiopia", "ET" },
            { "Europe", "EU" }, //not a real one, but necessary
            { "European Monetary Union", "EU" },
            { "Falkland Islands", "FK" },
            { "Faroe Islands", "FO" },
            { "Fiji", "FJ" },
            { "Finland", "FI" },
            { "France", "FR" },
            { "French Guiana", "GF" },
            { "French Polynesia", "PF" },
            { "French Southern Territories", "TF" },
            { "Gabon", "GA" },
            { "Gambia", "GM" },
            { "Georgia", "GE" },
            { "Germany", "DE" },
            { "Ghana", "GH" },
            { "Gibraltar", "GI" },
            { "Greece", "GR" },
            { "Greenland", "GL" },
            { "Grenada", "GD" },
            { "Guadeloupe", "GP" },
            { "Guam", "GU" },
            { "Guatemala", "GT" },
            { "Guernsey", "GG" },
            { "Guinea", "GN" },
            { "Guinea-Bissau", "GW" },
            { "Guyana", "GY" },
            { "Haiti", "HT" },
            { "Heard & McDonald Islands", "HM" },
            { "Honduras", "HN" },
            { "Hong Kong", "HK" },
            { "Hong Kong SAR", "HK" },
            { "Hungary", "HU" },
            { "Iceland", "IS" },
            { "India", "IN" },
            { "Indonesia", "ID" },
            { "Iran", "IR" },
            { "Iraq", "IQ" },
            { "Ireland", "IE" },
            { "Isle of Man", "IM" },
            { "Israel", "IL" },
            { "Italy", "IT" },
            { "Jamaica", "JM" },
            { "Japan", "JP" },
            { "Jersey", "JE" },
            { "Jordan", "JO" },
            { "Kazakhstan", "KZ" },
            { "Kenya", "KE" },
            { "Kiribati", "KI" },
            { "Kuwait", "KW" },
            { "Kyrgyzstan", "KG" },
            { "Laos", "LA" },
            { "Latvia", "LV" },
            { "Lebanon", "LB" },
            { "Lesotho", "LS" },
            { "Liberia", "LR" },
            { "Libya", "LY" },
            { "Liechtenstein", "LI" },
            { "Lithuania", "LT" },
            { "Luxembourg", "LU" },
            { "Macau", "MO" },
            { "Macedonia", "MK" },
            { "Madagascar", "MG" },
            { "Malawi", "MW" },
            { "Malaysia", "MY" },
            { "Maldives", "MV" },
            { "Mali", "ML" },
            { "Malta", "MT" },
            { "Marshall Islands", "MH" },
            { "Martinique", "MQ" },
            { "Mauritania", "MR" },
            { "Mauritius", "MU" },
            { "Mayotte", "YT" },
            { "Mexico", "MX" },
            { "Micronesia", "FM" },
            { "Moldova", "MD" },
            { "Monaco", "MC" },
            { "Mongolia", "MN" },
            { "Montenegro", "ME" },
            { "Montserrat", "MS" },
            { "Morocco", "MA" },
            { "Mozambique", "MZ" },
            { "Myanmar", "MM" },
            { "Namibia", "NA" },
            { "Nauru", "NR" },
            { "Nepal", "NP" },
            { "Netherlands", "NL" },
            { "The Netherlands", "NL" },
            { "Netherlands, The", "NL" },
            { "New Caledonia", "NC" },
            { "New Zealand", "NZ" },
            { "Nicaragua", "NI" },
            { "Niger", "NE" },
            { "Nigeria", "NG" },
            { "Niue", "NU" },
            { "Norfolk Island", "NF" },
            { "North Korea", "KP" },
            { "Northern Mariana Islands", "MP" },
            { "Norway", "NO" },
            { "Oman", "OM" },
            { "Pakistan", "PK" },
            { "Palau", "PW" },
            { "Palestine", "PS" },
            { "Panama", "PA" },
            { "Papua New Guinea", "PG" },
            { "Paraguay", "PY" },
            { "Peru", "PE" },
            { "Philippines", "PH" },
            { "Pitcairn Islands", "PN" },
            { "Poland", "PL" },
            { "Portugal", "PT" },
            { "Puerto Rico", "PR" },
            { "Qatar", "QA" },
            { "Romania", "RO" },
            { "Russia", "RU" },
            { "Rwanda", "RW" },
            { "Réunion", "RE" },
            { "Samoa", "WS" },
            { "San Marino", "SM" },
            { "Saudi Arabia", "SA" },
            { "Senegal", "SN" },
            { "Serbia", "RS" },
            { "Seychelles", "SC" },
            { "Sierra Leone", "SL" },
            { "Singapore", "SG" },
            { "Sint Maarten", "SX" },
            { "Slovakia", "SK" },
            { "Slovenia", "SI" },
            { "Solomon Islands", "SB" },
            { "Somalia", "SO" },
            { "South Africa", "ZA" },
            { "South Georgia & South Sandwich Islands", "GS" },
            { "South Korea", "KR" },
            { "South Sudan", "SS" },
            { "Spain", "ES" },
            { "Sri Lanka", "LK" },
            { "St. Barthélemy", "BL" },
            { "St. Helena", "SH" },
            { "St. Kitts & Nevis", "KN" },
            { "St. Lucia", "LC" },
            { "St. Martin", "MF" },
            { "St. Pierre & Miquelon", "PM" },
            { "St. Vincent & Grenadines", "VC" },
            { "Sudan", "SD" },
            { "Suriname", "SR" },
            { "Svalbard & Jan Mayen", "SJ" },
            { "Swaziland", "SZ" },
            { "Sweden", "SE" },
            { "Switzerland", "CH" },
            { "Syria", "SY" },
            { "São Tomé & Príncipe", "ST" },
            { "Taiwan", "TW" },
            { "Tajikistan", "TJ" },
            { "Tanzania", "TZ" },
            { "Thailand", "TH" },
            { "Timor-Leste", "TL" },
            { "Togo", "TG" },
            { "Tokelau", "TK" },
            { "Tonga", "TO" },
            { "Trinidad & Tobago", "TT" },
            { "Tunisia", "TN" },
            { "Turkey", "TR" },
            { "Turkmenistan", "TM" },
            { "Turks & Caicos Islands", "TC" },
            { "Tuvalu", "TV" },
            { "U.S. Outlying Islands", "UM" },
            { "U.S. Virgin Islands", "VI" },
            { "UK", "GB" },
            { "U.K.", "GB" },
            { "United Kingdom", "GB" },
            { "US", "US" },
            { "U.S.", "US" },
            { "USA", "US" },
            { "U.S.A.", "US" },
            { "United States", "US" },
            { "United States of America", "US" },
            { "Uganda", "UG" },
            { "Ukraine", "UA" },
            { "United Arab Emirates", "AE" },
            { "Uruguay", "UY" },
            { "Uzbekistan", "UZ" },
            { "Vanuatu", "VU" },
            { "Vatican City", "VA" },
            { "Venezuela", "VE" },
            { "Vietnam", "VN" },
            { "Wallis & Futuna", "WF" },
            { "Western Sahara", "EH" },
            { "Yemen", "YE" },
            { "Zambia", "ZM" },
            { "Zimbabwe", "ZW" },
            { "Åland Islands", "AX" }
        };

        /// <summary>
        /// Returns ISO 3166 ALPHA-2 Country Code
        /// </summary>
        /// <param name="countryName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="countryName"/> is <see langword="null" />.</exception>
        public static string CountryNameToCountryCode(string countryName)
        {
            if (string.IsNullOrEmpty(countryName)) throw new ArgumentNullException(nameof(countryName));

            return CountryCodes.ContainsKey(countryName) ? CountryCodes[countryName] : "--";
        }

        private static readonly Dictionary<string, string> CurrencyCodes = new Dictionary<string, string>
        {
            { "AF", "AFN" },
            { "AL", "ALL" },
            { "DZ", "DZD" },
            { "AS", "USD" },
            { "AD", "EUR" },
            { "AO", "AOA" },
            { "AI", "XCD" },
            { "AQ", "" },
            { "AG", "XCD" },
            { "AR", "ARS" },
            { "AM", "AMD" },
            { "AW", "AWG" },
            { "AU", "AUD" },
            { "AT", "EUR" },
            { "AZ", "AZN" },
            { "BS", "BSD" },
            { "BH", "BHD" },
            { "BD", "BDT" },
            { "BB", "BBD" },
            { "BY", "BYR" },
            { "BE", "EUR" },
            { "BZ", "BZD" },
            { "BJ", "XOF" },
            { "BM", "BMD" },
            { "BT", "INR" },
            { "BO", "BOB" },
            { "BA", "BAM" },
            { "BW", "BWP" },
            { "BV", "" },
            { "BR", "BRL" },
            { "IO", "" },
            { "VG", "USD" },
            { "BN", "BND" },
            { "BG", "BGN" },
            { "BF", "XOF" },
            { "BI", "BIF" },
            { "KH", "KHR" },
            { "CM", "XAF" },
            { "CA", "CAD" },
            { "CV", "CVE" },
            { "BQ", "USD" },
            { "KY", "KYD" },
            { "CF", "XAF" },
            { "TD", "XAF" },
            { "CL", "CLP" },
            { "CN", "CNY" },
            { "CX", "" },
            { "CC", "" },
            { "CO", "COP" },
            { "KM", "KMF" },
            { "CG", "XAF" },
            { "CD", "" },
            { "CK", "NZD" },
            { "CR", "CRC" },
            { "HR", "HRK" },
            { "CU", "CUP" },
            { "CW", "ANG" },
            { "CY", "EUR" },
            { "CZ", "CZK" },
            { "CI", "XOF" },
            { "DK", "DKK" },
            { "DJ", "DJF" },
            { "DM", "XCD" },
            { "DO", "DOP" },
            { "EC", "USD" },
            { "EG", "EGP" },
            { "SV", "USD" },
            { "GQ", "XAF" },
            { "ER", "ERN" },
            { "EE", "EUR" },
            { "ET", "ETB" },
            { "FK", "FKP" },
            { "FO", "" },
            { "FJ", "FJD" },
            { "FI", "EUR" },
            { "FR", "EUR" },
            { "GF", "EUR" },
            { "PF", "XPF" },
            { "TF", "" },
            { "GA", "XAF" },
            { "GM", "GMD" },
            { "GE", "GEL" },
            { "DE", "EUR" },
            { "GH", "GHS" },
            { "GI", "GIP" },
            { "GR", "EUR" },
            { "GL", "DKK" },
            { "GD", "XCD" },
            { "GP", "EUR" },
            { "GU", "USD" },
            { "GT", "GTQ" },
            { "GG", "GBP" },
            { "GN", "GNF" },
            { "GW", "XOF" },
            { "GY", "GYD" },
            { "HT", "USD" },
            { "HM", "" },
            { "HN", "HNL" },
            { "HK", "HKD" },
            { "HU", "HUF" },
            { "IS", "ISK" },
            { "IN", "INR" },
            { "ID", "IDR" },
            { "IR", "IRR" },
            { "IQ", "IQD" },
            { "IE", "EUR" },
            { "IM", "GBP" },
            { "IL", "ILS" },
            { "IT", "EUR" },
            { "JM", "JMD" },
            { "JP", "JPY" },
            { "JE", "GBP" },
            { "JO", "JOD" },
            { "KZ", "KZT" },
            { "KE", "KES" },
            { "KI", "AUD" },
            { "KW", "KWD" },
            { "KG", "KGS" },
            { "LA", "LAK" },
            { "LV", "EUR" },
            { "LB", "LBP" },
            { "LS", "ZAR" },
            { "LR", "LRD" },
            { "LY", "LYD" },
            { "LI", "CHF" },
            { "LT", "EUR" },
            { "LU", "EUR" },
            { "MO", "MOP" },
            { "MK", "MKD" },
            { "MG", "MGA" },
            { "MW", "MWK" },
            { "MY", "MYR" },
            { "MV", "MVR" },
            { "ML", "XOF" },
            { "MT", "EUR" },
            { "MH", "USD" },
            { "MQ", "EUR" },
            { "MR", "MRO" },
            { "MU", "MUR" },
            { "YT", "EUR" },
            { "MX", "MXN" },
            { "FM", "USD" },
            { "MD", "MDL" },
            { "MC", "EUR" },
            { "MN", "MNT" },
            { "ME", "EUR" },
            { "MS", "XCD" },
            { "MA", "MAD" },
            { "MZ", "MZN" },
            { "MM", "MMK" },
            { "NA", "ZAR" },
            { "NR", "AUD" },
            { "NP", "NPR" },
            { "NL", "EUR" },
            { "NC", "XPF" },
            { "NZ", "NZD" },
            { "NI", "NIO" },
            { "NE", "XOF" },
            { "NG", "NGN" },
            { "NU", "NZD" },
            { "NF", "AUD" },
            { "KP", "KPW" },
            { "MP", "USD" },
            { "NO", "NOK" },
            { "OM", "OMR" },
            { "PK", "PKR" },
            { "PW", "USD" },
            { "PS", "" },
            { "PA", "USD" },
            { "PG", "PGK" },
            { "PY", "PYG" },
            { "PE", "PEN" },
            { "PH", "PHP" },
            { "PN", "NZD" },
            { "PL", "PLN" },
            { "PT", "EUR" },
            { "PR", "USD" },
            { "QA", "QAR" },
            { "RO", "RON" },
            { "RU", "RUB" },
            { "RW", "RWF" },
            { "RE", "EUR" },
            { "WS", "WST" },
            { "SM", "EUR" },
            { "SA", "SAR" },
            { "SN", "XOF" },
            { "RS", "RSD" },
            { "SC", "SCR" },
            { "SL", "SLL" },
            { "SG", "SGD" },
            { "SX", "ANG" },
            { "SK", "EUR" },
            { "SI", "EUR" },
            { "SB", "SBD" },
            { "SO", "SOS" },
            { "ZA", "ZAR" },
            { "GS", "" },
            { "KR", "KRW" },
            { "SS", "SSP" },
            { "ES", "EUR" },
            { "LK", "LKR" },
            { "BL", "EUR" },
            { "SH", "SHP" },
            { "KN", "XCD" },
            { "LC", "XCD" },
            { "MF", "EUR" },
            { "PM", "EUR" },
            { "VC", "XCD" },
            { "SD", "SDG" },
            { "SR", "SRD" },
            { "SJ", "NOK" },
            { "SZ", "SZL" },
            { "SE", "SEK" },
            { "CH", "CHF" },
            { "SY", "SYP" },
            { "ST", "STD" },
            { "TW", "TWD" },
            { "TJ", "TJS" },
            { "TZ", "TZS" },
            { "TH", "THB" },
            { "TL", "USD" },
            { "TG", "XOF" },
            { "TK", "NZD" },
            { "TO", "TOP" },
            { "TT", "TTD" },
            { "TN", "TND" },
            { "TR", "TRY" },
            { "TM", "TMT" },
            { "TC", "USD" },
            { "TV", "AUD" },
            { "UM", "" },
            { "VI", "USD" },
            { "GB", "GBP" },
            { "US", "USD" },
            { "UG", "UGX" },
            { "UA", "UAH" },
            { "AE", "AED" },
            { "UY", "UYU" },
            { "UZ", "UZS" },
            { "VU", "VUV" },
            { "VA", "EUR" },
            { "VE", "VEF" },
            { "VN", "VND" },
            { "WF", "XPF" },
            { "EH", "MAD" },
            { "YE", "YER" },
            { "ZM", "ZMW" },
            { "ZW", "ZWL" },
            { "AX", "EUR" },
            { "EU", "EUR" }
        };

        /// <summary>
        /// Returns ISO 4217 3-letter currency code
        /// </summary>
        /// <param name="countryCode">2-letter ISO country code</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="countryCode"/> is <see langword="null" />.</exception>
        public static string CountryCodeToCurrencyCode(string countryCode)
        {
            if (string.IsNullOrEmpty(countryCode)) throw new ArgumentNullException(nameof(countryCode));

            return CurrencyCodes.ContainsKey(countryCode) ? CurrencyCodes[countryCode] : "N/A";
        }
    }
}