// -----------------------------------------------------------------------
// <copyright file="Seed.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity.Migrations;
using EntityData;
using QDMS;

namespace QDMSServer
{
    public static class Seed
    {
        /// <summary>
        /// This list is here for use in tests
        /// </summary>
        public static List<Country> Countries = new List<Country>
            {
                new Country { Name = "Afghanistan", CountryCode = "AF", CurrencyCode = "AFN" },
                new Country { Name = "Albania", CountryCode = "AL", CurrencyCode = "ALL" },
                new Country { Name = "Algeria", CountryCode = "DZ", CurrencyCode = "DZD" },
                new Country { Name = "American Samoa", CountryCode = "AS", CurrencyCode = "USD" },
                new Country { Name = "Andorra", CountryCode = "AD", CurrencyCode = "EUR" },
                new Country { Name = "Angola", CountryCode = "AO", CurrencyCode = "AOA" },
                new Country { Name = "Anguilla", CountryCode = "AI", CurrencyCode = "XCD" },
                new Country { Name = "Antigua & Barbuda", CountryCode = "AG", CurrencyCode = "XCD" },
                new Country { Name = "Argentina", CountryCode = "AR", CurrencyCode = "ARS" },
                new Country { Name = "Armenia", CountryCode = "AM", CurrencyCode = "AMD" },
                new Country { Name = "Aruba", CountryCode = "AW", CurrencyCode = "AWG" },
                new Country { Name = "Australia", CountryCode = "AU", CurrencyCode = "AUD" },
                new Country { Name = "Austria", CountryCode = "AT", CurrencyCode = "EUR" },
                new Country { Name = "Azerbaijan", CountryCode = "AZ", CurrencyCode = "AZN" },
                new Country { Name = "Bahamas", CountryCode = "BS", CurrencyCode = "BSD" },
                new Country { Name = "Bahrain", CountryCode = "BH", CurrencyCode = "BHD" },
                new Country { Name = "Bangladesh", CountryCode = "BD", CurrencyCode = "BDT" },
                new Country { Name = "Barbados", CountryCode = "BB", CurrencyCode = "BBD" },
                new Country { Name = "Belarus", CountryCode = "BY", CurrencyCode = "BYR" },
                new Country { Name = "Belgium", CountryCode = "BE", CurrencyCode = "EUR" },
                new Country { Name = "Belize", CountryCode = "BZ", CurrencyCode = "BZD" },
                new Country { Name = "Benin", CountryCode = "BJ", CurrencyCode = "XOF" },
                new Country { Name = "Bermuda", CountryCode = "BM", CurrencyCode = "BMD" },
                new Country { Name = "Bhutan", CountryCode = "BT", CurrencyCode = "INR" },
                new Country { Name = "Bolivia", CountryCode = "BO", CurrencyCode = "BOB" },
                new Country { Name = "Bosnia", CountryCode = "BA", CurrencyCode = "BAM" },
                new Country { Name = "Botswana", CountryCode = "BW", CurrencyCode = "BWP" },
                new Country { Name = "Brazil", CountryCode = "BR", CurrencyCode = "BRL" },
                new Country { Name = "British Virgin Islands", CountryCode = "VG", CurrencyCode = "USD" },
                new Country { Name = "Brunei", CountryCode = "BN", CurrencyCode = "BND" },
                new Country { Name = "Bulgaria", CountryCode = "BG", CurrencyCode = "BGN" },
                new Country { Name = "Burkina Faso", CountryCode = "BF", CurrencyCode = "XOF" },
                new Country { Name = "Burundi", CountryCode = "BI", CurrencyCode = "BIF" },
                new Country { Name = "Cambodia", CountryCode = "KH", CurrencyCode = "KHR" },
                new Country { Name = "Cameroon", CountryCode = "CM", CurrencyCode = "XAF" },
                new Country { Name = "Canada", CountryCode = "CA", CurrencyCode = "CAD" },
                new Country { Name = "Cape Verde", CountryCode = "CV", CurrencyCode = "CVE" },
                new Country { Name = "Caribbean Netherlands", CountryCode = "BQ", CurrencyCode = "USD" },
                new Country { Name = "Cayman Islands", CountryCode = "KY", CurrencyCode = "KYD" },
                new Country { Name = "Central African Republic", CountryCode = "CF", CurrencyCode = "XAF" },
                new Country { Name = "Chad", CountryCode = "TD", CurrencyCode = "XAF" },
                new Country { Name = "Chile", CountryCode = "CL", CurrencyCode = "CLP" },
                new Country { Name = "China", CountryCode = "CN", CurrencyCode = "CNY" },
                new Country { Name = "Colombia", CountryCode = "CO", CurrencyCode = "COP" },
                new Country { Name = "Comoros", CountryCode = "KM", CurrencyCode = "KMF" },
                new Country { Name = "Congo - Brazzaville", CountryCode = "CG", CurrencyCode = "XAF" },
                new Country { Name = "Cook Islands", CountryCode = "CK", CurrencyCode = "NZD" },
                new Country { Name = "Costa Rica", CountryCode = "CR", CurrencyCode = "CRC" },
                new Country { Name = "Croatia", CountryCode = "HR", CurrencyCode = "HRK" },
                new Country { Name = "Cuba", CountryCode = "CU", CurrencyCode = "CUP" },
                new Country { Name = "Curaçao", CountryCode = "CW", CurrencyCode = "ANG" },
                new Country { Name = "Cyprus", CountryCode = "CY", CurrencyCode = "EUR" },
                new Country { Name = "Czech Republic", CountryCode = "CZ", CurrencyCode = "CZK" },
                new Country { Name = "Côte d’Ivoire", CountryCode = "CI", CurrencyCode = "XOF" },
                new Country { Name = "Denmark", CountryCode = "DK", CurrencyCode = "DKK" },
                new Country { Name = "Djibouti", CountryCode = "DJ", CurrencyCode = "DJF" },
                new Country { Name = "Dominica", CountryCode = "DM", CurrencyCode = "XCD" },
                new Country { Name = "Dominican Republic", CountryCode = "DO", CurrencyCode = "DOP" },
                new Country { Name = "Ecuador", CountryCode = "EC", CurrencyCode = "USD" },
                new Country { Name = "Egypt", CountryCode = "EG", CurrencyCode = "EGP" },
                new Country { Name = "El Salvador", CountryCode = "SV", CurrencyCode = "USD" },
                new Country { Name = "Equatorial Guinea", CountryCode = "GQ", CurrencyCode = "XAF" },
                new Country { Name = "Eritrea", CountryCode = "ER", CurrencyCode = "ERN" },
                new Country { Name = "Estonia", CountryCode = "EE", CurrencyCode = "EUR" },
                new Country { Name = "Ethiopia", CountryCode = "ET", CurrencyCode = "ETB" },
                new Country { Name = "Europe", CountryCode = "EU", CurrencyCode = "EUR" },
                new Country { Name = "European Monetary Union", CountryCode = "EU", CurrencyCode = "EUR" },
                new Country { Name = "Falkland Islands", CountryCode = "FK", CurrencyCode = "FKP" },
                new Country { Name = "Fiji", CountryCode = "FJ", CurrencyCode = "FJD" },
                new Country { Name = "Finland", CountryCode = "FI", CurrencyCode = "EUR" },
                new Country { Name = "France", CountryCode = "FR", CurrencyCode = "EUR" },
                new Country { Name = "French Guiana", CountryCode = "GF", CurrencyCode = "EUR" },
                new Country { Name = "French Polynesia", CountryCode = "PF", CurrencyCode = "XPF" },
                new Country { Name = "Gabon", CountryCode = "GA", CurrencyCode = "XAF" },
                new Country { Name = "Gambia", CountryCode = "GM", CurrencyCode = "GMD" },
                new Country { Name = "Georgia", CountryCode = "GE", CurrencyCode = "GEL" },
                new Country { Name = "Germany", CountryCode = "DE", CurrencyCode = "EUR" },
                new Country { Name = "Ghana", CountryCode = "GH", CurrencyCode = "GHS" },
                new Country { Name = "Gibraltar", CountryCode = "GI", CurrencyCode = "GIP" },
                new Country { Name = "Greece", CountryCode = "GR", CurrencyCode = "EUR" },
                new Country { Name = "Greenland", CountryCode = "GL", CurrencyCode = "DKK" },
                new Country { Name = "Grenada", CountryCode = "GD", CurrencyCode = "XCD" },
                new Country { Name = "Guadeloupe", CountryCode = "GP", CurrencyCode = "EUR" },
                new Country { Name = "Guam", CountryCode = "GU", CurrencyCode = "USD" },
                new Country { Name = "Guatemala", CountryCode = "GT", CurrencyCode = "GTQ" },
                new Country { Name = "Guernsey", CountryCode = "GG", CurrencyCode = "GBP" },
                new Country { Name = "Guinea", CountryCode = "GN", CurrencyCode = "GNF" },
                new Country { Name = "Guinea-Bissau", CountryCode = "GW", CurrencyCode = "XOF" },
                new Country { Name = "Guyana", CountryCode = "GY", CurrencyCode = "GYD" },
                new Country { Name = "Haiti", CountryCode = "HT", CurrencyCode = "USD" },
                new Country { Name = "Honduras", CountryCode = "HN", CurrencyCode = "HNL" },
                new Country { Name = "Hong Kong", CountryCode = "HK", CurrencyCode = "HKD" },
                new Country { Name = "Hong Kong SAR", CountryCode = "HK", CurrencyCode = "HKD" },
                new Country { Name = "Hungary", CountryCode = "HU", CurrencyCode = "HUF" },
                new Country { Name = "Iceland", CountryCode = "IS", CurrencyCode = "ISK" },
                new Country { Name = "India", CountryCode = "IN", CurrencyCode = "INR" },
                new Country { Name = "Indonesia", CountryCode = "ID", CurrencyCode = "IDR" },
                new Country { Name = "Iran", CountryCode = "IR", CurrencyCode = "IRR" },
                new Country { Name = "Iraq", CountryCode = "IQ", CurrencyCode = "IQD" },
                new Country { Name = "Ireland", CountryCode = "IE", CurrencyCode = "EUR" },
                new Country { Name = "Isle of Man", CountryCode = "IM", CurrencyCode = "GBP" },
                new Country { Name = "Israel", CountryCode = "IL", CurrencyCode = "ILS" },
                new Country { Name = "Italy", CountryCode = "IT", CurrencyCode = "EUR" },
                new Country { Name = "Jamaica", CountryCode = "JM", CurrencyCode = "JMD" },
                new Country { Name = "Japan", CountryCode = "JP", CurrencyCode = "JPY" },
                new Country { Name = "Jersey", CountryCode = "JE", CurrencyCode = "GBP" },
                new Country { Name = "Jordan", CountryCode = "JO", CurrencyCode = "JOD" },
                new Country { Name = "Kazakhstan", CountryCode = "KZ", CurrencyCode = "KZT" },
                new Country { Name = "Kenya", CountryCode = "KE", CurrencyCode = "KES" },
                new Country { Name = "Kiribati", CountryCode = "KI", CurrencyCode = "AUD" },
                new Country { Name = "Kuwait", CountryCode = "KW", CurrencyCode = "KWD" },
                new Country { Name = "Kyrgyzstan", CountryCode = "KG", CurrencyCode = "KGS" },
                new Country { Name = "Laos", CountryCode = "LA", CurrencyCode = "LAK" },
                new Country { Name = "Latvia", CountryCode = "LV", CurrencyCode = "EUR" },
                new Country { Name = "Lebanon", CountryCode = "LB", CurrencyCode = "LBP" },
                new Country { Name = "Lesotho", CountryCode = "LS", CurrencyCode = "ZAR" },
                new Country { Name = "Liberia", CountryCode = "LR", CurrencyCode = "LRD" },
                new Country { Name = "Libya", CountryCode = "LY", CurrencyCode = "LYD" },
                new Country { Name = "Liechtenstein", CountryCode = "LI", CurrencyCode = "CHF" },
                new Country { Name = "Lithuania", CountryCode = "LT", CurrencyCode = "EUR" },
                new Country { Name = "Luxembourg", CountryCode = "LU", CurrencyCode = "EUR" },
                new Country { Name = "Macau", CountryCode = "MO", CurrencyCode = "MOP" },
                new Country { Name = "Macedonia", CountryCode = "MK", CurrencyCode = "MKD" },
                new Country { Name = "Madagascar", CountryCode = "MG", CurrencyCode = "MGA" },
                new Country { Name = "Malawi", CountryCode = "MW", CurrencyCode = "MWK" },
                new Country { Name = "Malaysia", CountryCode = "MY", CurrencyCode = "MYR" },
                new Country { Name = "Maldives", CountryCode = "MV", CurrencyCode = "MVR" },
                new Country { Name = "Mali", CountryCode = "ML", CurrencyCode = "XOF" },
                new Country { Name = "Malta", CountryCode = "MT", CurrencyCode = "EUR" },
                new Country { Name = "Marshall Islands", CountryCode = "MH", CurrencyCode = "USD" },
                new Country { Name = "Martinique", CountryCode = "MQ", CurrencyCode = "EUR" },
                new Country { Name = "Mauritania", CountryCode = "MR", CurrencyCode = "MRO" },
                new Country { Name = "Mauritius", CountryCode = "MU", CurrencyCode = "MUR" },
                new Country { Name = "Mayotte", CountryCode = "YT", CurrencyCode = "EUR" },
                new Country { Name = "Mexico", CountryCode = "MX", CurrencyCode = "MXN" },
                new Country { Name = "Micronesia", CountryCode = "FM", CurrencyCode = "USD" },
                new Country { Name = "Moldova", CountryCode = "MD", CurrencyCode = "MDL" },
                new Country { Name = "Monaco", CountryCode = "MC", CurrencyCode = "EUR" },
                new Country { Name = "Mongolia", CountryCode = "MN", CurrencyCode = "MNT" },
                new Country { Name = "Montenegro", CountryCode = "ME", CurrencyCode = "EUR" },
                new Country { Name = "Montserrat", CountryCode = "MS", CurrencyCode = "XCD" },
                new Country { Name = "Morocco", CountryCode = "MA", CurrencyCode = "MAD" },
                new Country { Name = "Mozambique", CountryCode = "MZ", CurrencyCode = "MZN" },
                new Country { Name = "Myanmar", CountryCode = "MM", CurrencyCode = "MMK" },
                new Country { Name = "Namibia", CountryCode = "NA", CurrencyCode = "ZAR" },
                new Country { Name = "Nauru", CountryCode = "NR", CurrencyCode = "AUD" },
                new Country { Name = "Nepal", CountryCode = "NP", CurrencyCode = "NPR" },
                new Country { Name = "Netherlands", CountryCode = "NL", CurrencyCode = "EUR" },
                new Country { Name = "The Netherlands", CountryCode = "NL", CurrencyCode = "EUR" },
                new Country { Name = "Netherlands, The", CountryCode = "NL", CurrencyCode = "EUR" },
                new Country { Name = "New Caledonia", CountryCode = "NC", CurrencyCode = "XPF" },
                new Country { Name = "New Zealand", CountryCode = "NZ", CurrencyCode = "NZD" },
                new Country { Name = "Nicaragua", CountryCode = "NI", CurrencyCode = "NIO" },
                new Country { Name = "Niger", CountryCode = "NE", CurrencyCode = "XOF" },
                new Country { Name = "Nigeria", CountryCode = "NG", CurrencyCode = "NGN" },
                new Country { Name = "Niue", CountryCode = "NU", CurrencyCode = "NZD" },
                new Country { Name = "Norfolk Island", CountryCode = "NF", CurrencyCode = "AUD" },
                new Country { Name = "North Korea", CountryCode = "KP", CurrencyCode = "KPW" },
                new Country { Name = "Northern Mariana Islands", CountryCode = "MP", CurrencyCode = "USD" },
                new Country { Name = "Norway", CountryCode = "NO", CurrencyCode = "NOK" },
                new Country { Name = "Oman", CountryCode = "OM", CurrencyCode = "OMR" },
                new Country { Name = "Pakistan", CountryCode = "PK", CurrencyCode = "PKR" },
                new Country { Name = "Palau", CountryCode = "PW", CurrencyCode = "USD" },
                new Country { Name = "Panama", CountryCode = "PA", CurrencyCode = "USD" },
                new Country { Name = "Papua New Guinea", CountryCode = "PG", CurrencyCode = "PGK" },
                new Country { Name = "Paraguay", CountryCode = "PY", CurrencyCode = "PYG" },
                new Country { Name = "Peru", CountryCode = "PE", CurrencyCode = "PEN" },
                new Country { Name = "Philippines", CountryCode = "PH", CurrencyCode = "PHP" },
                new Country { Name = "Pitcairn Islands", CountryCode = "PN", CurrencyCode = "NZD" },
                new Country { Name = "Poland", CountryCode = "PL", CurrencyCode = "PLN" },
                new Country { Name = "Portugal", CountryCode = "PT", CurrencyCode = "EUR" },
                new Country { Name = "Puerto Rico", CountryCode = "PR", CurrencyCode = "USD" },
                new Country { Name = "Qatar", CountryCode = "QA", CurrencyCode = "QAR" },
                new Country { Name = "Romania", CountryCode = "RO", CurrencyCode = "RON" },
                new Country { Name = "Russia", CountryCode = "RU", CurrencyCode = "RUB" },
                new Country { Name = "Rwanda", CountryCode = "RW", CurrencyCode = "RWF" },
                new Country { Name = "Réunion", CountryCode = "RE", CurrencyCode = "EUR" },
                new Country { Name = "Samoa", CountryCode = "WS", CurrencyCode = "WST" },
                new Country { Name = "San Marino", CountryCode = "SM", CurrencyCode = "EUR" },
                new Country { Name = "Saudi Arabia", CountryCode = "SA", CurrencyCode = "SAR" },
                new Country { Name = "Senegal", CountryCode = "SN", CurrencyCode = "XOF" },
                new Country { Name = "Serbia", CountryCode = "RS", CurrencyCode = "RSD" },
                new Country { Name = "Seychelles", CountryCode = "SC", CurrencyCode = "SCR" },
                new Country { Name = "Sierra Leone", CountryCode = "SL", CurrencyCode = "SLL" },
                new Country { Name = "Singapore", CountryCode = "SG", CurrencyCode = "SGD" },
                new Country { Name = "Sint Maarten", CountryCode = "SX", CurrencyCode = "ANG" },
                new Country { Name = "Slovakia", CountryCode = "SK", CurrencyCode = "EUR" },
                new Country { Name = "Slovenia", CountryCode = "SI", CurrencyCode = "EUR" },
                new Country { Name = "Solomon Islands", CountryCode = "SB", CurrencyCode = "SBD" },
                new Country { Name = "Somalia", CountryCode = "SO", CurrencyCode = "SOS" },
                new Country { Name = "South Africa", CountryCode = "ZA", CurrencyCode = "ZAR" },
                new Country { Name = "South Korea", CountryCode = "KR", CurrencyCode = "KRW" },
                new Country { Name = "South Sudan", CountryCode = "SS", CurrencyCode = "SSP" },
                new Country { Name = "Spain", CountryCode = "ES", CurrencyCode = "EUR" },
                new Country { Name = "Sri Lanka", CountryCode = "LK", CurrencyCode = "LKR" },
                new Country { Name = "St. Barthélemy", CountryCode = "BL", CurrencyCode = "EUR" },
                new Country { Name = "St. Helena", CountryCode = "SH", CurrencyCode = "SHP" },
                new Country { Name = "St. Kitts & Nevis", CountryCode = "KN", CurrencyCode = "XCD" },
                new Country { Name = "St. Lucia", CountryCode = "LC", CurrencyCode = "XCD" },
                new Country { Name = "St. Martin", CountryCode = "MF", CurrencyCode = "EUR" },
                new Country { Name = "St. Pierre & Miquelon", CountryCode = "PM", CurrencyCode = "EUR" },
                new Country { Name = "St. Vincent & Grenadines", CountryCode = "VC", CurrencyCode = "XCD" },
                new Country { Name = "Sudan", CountryCode = "SD", CurrencyCode = "SDG" },
                new Country { Name = "Suriname", CountryCode = "SR", CurrencyCode = "SRD" },
                new Country { Name = "Svalbard & Jan Mayen", CountryCode = "SJ", CurrencyCode = "NOK" },
                new Country { Name = "Swaziland", CountryCode = "SZ", CurrencyCode = "SZL" },
                new Country { Name = "Sweden", CountryCode = "SE", CurrencyCode = "SEK" },
                new Country { Name = "Switzerland", CountryCode = "CH", CurrencyCode = "CHF" },
                new Country { Name = "Syria", CountryCode = "SY", CurrencyCode = "SYP" },
                new Country { Name = "São Tomé & Príncipe", CountryCode = "ST", CurrencyCode = "STD" },
                new Country { Name = "Taiwan", CountryCode = "TW", CurrencyCode = "TWD" },
                new Country { Name = "Tajikistan", CountryCode = "TJ", CurrencyCode = "TJS" },
                new Country { Name = "Tanzania", CountryCode = "TZ", CurrencyCode = "TZS" },
                new Country { Name = "Thailand", CountryCode = "TH", CurrencyCode = "THB" },
                new Country { Name = "Timor-Leste", CountryCode = "TL", CurrencyCode = "USD" },
                new Country { Name = "Togo", CountryCode = "TG", CurrencyCode = "XOF" },
                new Country { Name = "Tokelau", CountryCode = "TK", CurrencyCode = "NZD" },
                new Country { Name = "Tonga", CountryCode = "TO", CurrencyCode = "TOP" },
                new Country { Name = "Trinidad & Tobago", CountryCode = "TT", CurrencyCode = "TTD" },
                new Country { Name = "Tunisia", CountryCode = "TN", CurrencyCode = "TND" },
                new Country { Name = "Turkey", CountryCode = "TR", CurrencyCode = "TRY" },
                new Country { Name = "Turkmenistan", CountryCode = "TM", CurrencyCode = "TMT" },
                new Country { Name = "Turks & Caicos Islands", CountryCode = "TC", CurrencyCode = "USD" },
                new Country { Name = "Tuvalu", CountryCode = "TV", CurrencyCode = "AUD" },
                new Country { Name = "U.S. Virgin Islands", CountryCode = "VI", CurrencyCode = "USD" },
                new Country { Name = "UK", CountryCode = "GB", CurrencyCode = "GBP" },
                new Country { Name = "U.K.", CountryCode = "GB", CurrencyCode = "GBP" },
                new Country { Name = "United Kingdom", CountryCode = "GB", CurrencyCode = "GBP" },
                new Country { Name = "US", CountryCode = "US", CurrencyCode = "USD" },
                new Country { Name = "U.S.", CountryCode = "US", CurrencyCode = "USD" },
                new Country { Name = "USA", CountryCode = "US", CurrencyCode = "USD" },
                new Country { Name = "U.S.A.", CountryCode = "US", CurrencyCode = "USD" },
                new Country { Name = "United States", CountryCode = "US", CurrencyCode = "USD" },
                new Country { Name = "United States of America", CountryCode = "US", CurrencyCode = "USD" },
                new Country { Name = "Uganda", CountryCode = "UG", CurrencyCode = "UGX" },
                new Country { Name = "Ukraine", CountryCode = "UA", CurrencyCode = "UAH" },
                new Country { Name = "United Arab Emirates", CountryCode = "AE", CurrencyCode = "AED" },
                new Country { Name = "Uruguay", CountryCode = "UY", CurrencyCode = "UYU" },
                new Country { Name = "Uzbekistan", CountryCode = "UZ", CurrencyCode = "UZS" },
                new Country { Name = "Vanuatu", CountryCode = "VU", CurrencyCode = "VUV" },
                new Country { Name = "Vatican City", CountryCode = "VA", CurrencyCode = "EUR" },
                new Country { Name = "Venezuela", CountryCode = "VE", CurrencyCode = "VEF" },
                new Country { Name = "Vietnam", CountryCode = "VN", CurrencyCode = "VND" },
                new Country { Name = "Wallis & Futuna", CountryCode = "WF", CurrencyCode = "XPF" },
                new Country { Name = "Western Sahara", CountryCode = "EH", CurrencyCode = "MAD" },
                new Country { Name = "Yemen", CountryCode = "YE", CurrencyCode = "YER" },
                new Country { Name = "Zambia", CountryCode = "ZM", CurrencyCode = "ZMW" },
                new Country { Name = "Zimbabwe", CountryCode = "ZW", CurrencyCode = "ZWL" },
                new Country { Name = "Åland Islands", CountryCode = "AX", CurrencyCode = "EUR" },

            };

        public static void SeedDatasources(MyDBContext context)
        {
            var ib = new Datasource { Name = "Interactive Brokers" };
            var yahoo = new Datasource { Name = "Yahoo" };
            var quandl = new Datasource { Name = "Quandl" };
            var fred = new Datasource { Name = "FRED" };
            var barChart = new Datasource { Name = "BarChart" };

            context.Datasources.AddOrUpdate(x => x.Name, ib, yahoo, quandl, fred, barChart);

            context.SaveChanges();
        }

        public static void DoSeed()
        {
            var context = new MyDBContext();

            #region underlyingSymbols
            var eur = new UnderlyingSymbol
            {
                Symbol = "6E",
                Rule = new ExpirationRule
                {
                    DaysBefore = 2,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceDayType = ReferenceDayType.WeekDays,
                    ReferenceWeekDay = DayOfTheWeek.Wednesday,
                    ReferenceWeekDayCount = WeekDayCount.Third
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, eur);

            var cl = new UnderlyingSymbol
            {
                Symbol = "CL",
                Rule = new ExpirationRule
                {
                    DaysBefore = 3,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.PreviousMonth,
                    ReferenceDayType = ReferenceDayType.CalendarDays,
                    ReferenceDays = 25,
                    ReferenceDayMustBeBusinessDay = true
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, cl);

            var zw = new UnderlyingSymbol
            {
                Symbol = "ZW",
                Rule = new ExpirationRule
                {
                    DaysBefore = 1,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceDayType = ReferenceDayType.CalendarDays,
                    ReferenceDays = 15
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, zw);

            var es = new UnderlyingSymbol
            {
                Symbol = "ES",
                Rule = new ExpirationRule
                {
                    DaysBefore = 0,
                    DayType = DayType.Calendar,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceDayType = ReferenceDayType.WeekDays,
                    ReferenceWeekDay = DayOfTheWeek.Friday,
                    ReferenceWeekDayCount = WeekDayCount.Third
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, es);


            var nq = new UnderlyingSymbol
            {
                Symbol = "NQ",
                Rule = new ExpirationRule
                {
                    DaysBefore = 0,
                    DayType = DayType.Calendar,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceDayType = ReferenceDayType.WeekDays,
                    ReferenceWeekDay = DayOfTheWeek.Friday,
                    ReferenceWeekDayCount = WeekDayCount.Third
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, nq);

            var gc = new UnderlyingSymbol
            {
                Symbol = "GC",
                Rule = new ExpirationRule
                {
                    DaysBefore = 2,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceDayType = ReferenceDayType.LastDayOfMonth
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, gc);

            var ub = new UnderlyingSymbol
            {
                Symbol = "UB",
                Rule = new ExpirationRule
                {
                    DaysBefore = 7,
                    DayType = DayType.Business,
                    ReferenceRelativeMonth = RelativeMonth.CurrentMonth,
                    ReferenceDayType = ReferenceDayType.LastDayOfMonth
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, ub);

            var vix = new UnderlyingSymbol
            {
                Symbol = "VIX",
                Rule = new ExpirationRule
                {
                    DaysBefore = 30,
                    DayType = DayType.Calendar,
                    ReferenceRelativeMonth = RelativeMonth.NextMonth,
                    ReferenceDayType = ReferenceDayType.WeekDays,
                    ReferenceWeekDay = DayOfTheWeek.Friday,
                    ReferenceWeekDayCount = WeekDayCount.Third,
                    ReferenceDayMustBeBusinessDay = true
                }
            };
            context.UnderlyingSymbols.AddOrUpdate(x => x.Symbol, vix);
            #endregion

            #region sessiontemplates
            var sessiontemplates = new []
            {
                new SessionTemplate { Name = "U.S. Equities RTH" },
                new SessionTemplate { Name = "U.S. Equities (w/ Post)" },
                new SessionTemplate { Name = "U.S. Equities (w/ Pre)" },
                new SessionTemplate { Name = "U.S. Equities (w/ Pre & Post)" },
                new SessionTemplate { Name = "CME: Equity Index Futures (GLOBEX)" },
                new SessionTemplate { Name = "CME: Equity Index Futures (Open Outcry)" },
                new SessionTemplate { Name = "CME: Equity Index Futures [E-Mini] (GLOBEX)" },
                new SessionTemplate { Name = "CME: FX Futures (GLOBEX)" },
            };
            foreach (SessionTemplate s in sessiontemplates)
            {
                context.SessionTemplates.AddOrUpdate(x => x.Name, s);
            }


            #endregion

            #region templatesessions
            var templatesessions = new[]
            {
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities RTH")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(9, 30, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 0, 0),
                    ClosingTime = new TimeSpan(20, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "U.S. Equities (w/ Pre & Post)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(8, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Sunday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(8, 30, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Friday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures (Open Outcry)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(15, 30, 0),
                    ClosingTime = new TimeSpan(16, 30, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(15, 15, 0),
                    IsSessionEnd = false,
                    OpeningDay = DayOfTheWeek.Sunday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: Equity Index Futures [E-Mini] (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Monday,
                    ClosingDay = DayOfTheWeek.Tuesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Tuesday,
                    ClosingDay = DayOfTheWeek.Wednesday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Wednesday,
                    ClosingDay = DayOfTheWeek.Thursday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Thursday,
                    ClosingDay = DayOfTheWeek.Friday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                },
                new TemplateSession
                {
                    OpeningTime = new TimeSpan(17, 0, 0),
                    ClosingTime = new TimeSpan(16, 0, 0),
                    IsSessionEnd = true,
                    OpeningDay = DayOfTheWeek.Sunday,
                    ClosingDay = DayOfTheWeek.Monday,
                    Template = sessiontemplates.First(x => x.Name == "CME: FX Futures (GLOBEX)")
                }
            };

            foreach (TemplateSession t in templatesessions)
            {
                context.TemplateSessions.Add(t);
            }
            #endregion

            #region exchanges
            var exchanges = new[] 
                {
                    new Exchange { Name = "AB", LongName = "American Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "AEB", LongName = "Euronext Netherlands", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ALSE", LongName = "Alberta Stock Exchange", Timezone = "Mountain Standard Time" },
                    new Exchange { Name = "AMEX", LongName = "American Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "AMS", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ANTSE", LongName = "Antwerp Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "AO", LongName = "American Stock Exchange (Options)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "API", LongName = "American Petroleum Institute", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ARCA", LongName = "Archipelago", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ARCX", LongName = "Archipelago Electronic Communications Network", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ASE", LongName = "Amsterdam Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ASX", LongName = "Australian Stock Exchange", Timezone = "AUS Eastern Standard Time" },
                    new Exchange { Name = "ASXI", LongName = "Australian Stock Exchange", Timezone = "AUS Eastern Standard Time" },
                    new Exchange { Name = "ATA", LongName = "AEX-Agrarische Termynmarkt", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ATAASE", LongName = "Athens Stock Exchange", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "ATH", LongName = "Athens Stock Exchange", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "ATHI", LongName = "Athens Stock Exchange", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "AUSSE", LongName = "Australian Stock Exchange", Timezone = "AUS Eastern Standard Time" },
                    new Exchange { Name = "B", LongName = "Boston Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BARB", LongName = "Barclays Bank", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BARSE", LongName = "Barcelona Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BASB", LongName = "Basle Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BASE", LongName = "Buenos Aires Stock Exchange", Timezone = "S.A. Eastern Standard Time" },
                    new Exchange { Name = "BASLE", LongName = "Basle Stocks", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BB", LongName = "Bulletin Board", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BELFOX", LongName = "Euronext Brussels", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BER", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BERNB", LongName = "Bern Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BERSE", LongName = "Berlin Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BET", LongName = "Budapest Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BFE", LongName = "Baltic Freight Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "BMBSE", LongName = "Bombay Stock Exchange", Timezone = "India Standard Time" },
                    new Exchange { Name = "BMF", LongName = "Bolsa Mecadario Futuro", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "BO", LongName = "Boston Options Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BOGSE", LongName = "Bogota Stock Exchange", Timezone = "S.A. Pacific Standard Time" },
                    new Exchange { Name = "BOX", LongName = "Boston Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BRN", LongName = "Swiss Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BRU", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BRUSE", LongName = "Brussels Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BRUT", LongName = "Brut", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BSE", LongName = "Bern Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BSP", LongName = "Sao Paulo Stock Exchange", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "BSPI", LongName = "Sao Paulo Stock Exchange", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "BT", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "BTRADE", LongName = "Bloomberg Tradebook", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BUD", LongName = "Budapest Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BUDI", LongName = "Budapest Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BUE", LongName = "Buenos Aires Stock Exchange", Timezone = "S.A. Eastern Standard Time" },
                    new Exchange { Name = "BUEI", LongName = "Buenos Aires Stock Exchange", Timezone = "S.A. Eastern Standard Time" },
                    new Exchange { Name = "BUTLR", LongName = "Butler Harlow", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BVME", LongName = "Italian Exchange - Cash Market", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "C", LongName = "Cincinnati Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CAES", LongName = "Computer Assisted Execution System", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CALC", LongName = "Calculated Indices", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CANX", LongName = "Canadian Mutual Funds (Cannex)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CARSE", LongName = "Caracas Stock Exchange", Timezone = "Pacific S.A. Standard Time" },
                    new Exchange { Name = "CBFX", LongName = "Chemical Bank Forex", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CBOE", LongName = "Chicago Board Options Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CBOT", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CBT", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CDE", LongName = "Canadian Derivatives Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CEC", LongName = "Comodities Exchange Center", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CEX", LongName = "Citibank Forex", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CF", LongName = "CBOE Futures Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CFE", LongName = "CBOE Futures Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CFFE", LongName = "Cantor Financial Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CFOREX", LongName = "Crossmar Foreign Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CFXT", LongName = "S&P Comstock Composite Forex", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CME", LongName = "Chicago Mercantile Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "CO", LongName = "Chicago Board Options Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "COATS", LongName = "Candian OTC Automated Trading System", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "COLSE", LongName = "Colombo Stock Exchange", Timezone = "Central Asia Standard Time" },
                    new Exchange { Name = "COMEX", LongName = "Commodity Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "COMP", LongName = "Composite Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "COMX", LongName = "Comex Metals", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "COPSE", LongName = "Copenhagen Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "CPC", LongName = "Computer Petroleum Corp", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CSC", LongName = "Coffee, Sugar, and Cocoa Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CSE", LongName = "Cincinnati Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CSEI", LongName = "Coppenhagen Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "D", LongName = "NASDAQ ADF", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "DBN", LongName = "STOXX Indices", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DBX", LongName = "Stuttgart Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DBXI", LongName = "Stuttgart Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DSE", LongName = "Dusseldorf Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DT", LongName = "Dow Jones", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "DTB", LongName = "EUREX", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "DUB", LongName = "Ireland Stock Exchange", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "DUS", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EBS", LongName = "Swiss Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EBSBW", LongName = "Swiss Market Feed's EBS Project  - Bonds & Warrant", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EBSSTK", LongName = "Swiss Market Feed's EBS Project - Stocks", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EC", LongName = "Chicago Board of Trade (E-mini)", Timezone = "Central Standard Time" },
                    new Exchange { Name = "ECBOT", LongName = "Chicago Board of Trade E-CBOT", Timezone = "Central Standard Time" },
                    new Exchange { Name = "EEB", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EEX", LongName = "European Energy Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EIBI", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EM", LongName = "Chicago Mercantile Exchange (E-mini)", Timezone = "Central Standard Time" },
                    new Exchange { Name = "EOE", LongName = "European Options Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ESE", LongName = "Edmonton Stock Exchange", Timezone = "Mountain Standard Time" },
                    new Exchange { Name = "EUREX", LongName = "Eurex Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EUREXUS", LongName = "Eurex US", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EUS", LongName = "Eurex US Futures", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "EUX", LongName = "Eurex", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "FOREX", LongName = "FOREX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "FOX", LongName = "London Future & Options Exchange", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "FRA", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "FTA", LongName = "Euronext NL", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "FTSA", LongName = "Athens FTSE Indices", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "FTSE", LongName = "FTSE Index Values", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "FTSJ", LongName = "Johannesburg FTSE Indices", Timezone = "South Africa Standard Time" },
                    new Exchange { Name = "FUKSE", LongName = "Fukuoaka Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "FWB", LongName = "Frankfurt Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "FX", LongName = "FOREX", Timezone = "Central Standard Time" },
                    new Exchange { Name = "GARVIN", LongName = "Garvin Bonds", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "GB", LongName = "ICAP (Garvin) Bonds", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "GENEVA", LongName = "Geneva Stocks", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "GENEVB", LongName = "Geneva Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "GLOBEX", LongName = "Chicago Mercantile Exchange (CME GLOBEX)", Timezone = "Central Standard Time" },
                    new Exchange { Name = "HAM", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "HAN", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "HEL", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HELI", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HELSE", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HIRSE", LongName = "Hiroshima Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "HKEX", LongName = "Hong Kong Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HKFE", LongName = "Hong Kong Futures Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HKG", LongName = "Hong Kong Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HKGI", LongName = "Hang Seng Indices", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "HKME", LongName = "Hong Kong Metals Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HKSE", LongName = "Hong Kong Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "HMBSE", LongName = "Hamburg Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "HSE", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HSEI", LongName = "Helsinski Stock Exchange", Timezone = "FLE Standard Time" },
                    new Exchange { Name = "HY", LongName = "", Timezone = "" },
                    new Exchange { Name = "IBIS", LongName = "Ibis", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ICE", LongName = "Intercontinental Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ICEI", LongName = "Iceland Stock Exchange", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "ICP", LongName = "ICAP OTC", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "IDEAL", LongName = "IDEAL IB FOREX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "IDEALPRO", LongName = "IDEAL IB FOREX PRO", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "IDEM", LongName = "Borsa Italiana", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "IDX", LongName = "World Indices", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "IGB", LongName = "Italian Government Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "INDEX", LongName = "", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "INSNET", LongName = "Instinet", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "INT3B", LongName = "", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "INT3P", LongName = "", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "IO", LongName = "International Securities Exchange (Options)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "IPE", LongName = "International Petroleum Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "IRISE", LongName = "Irish Stock Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "ISE", LongName = "Istanbul Stock Exchange", Timezone = "GTB Standard Time" },
                    new Exchange { Name = "ISEI", LongName = "Iceland Stock Exchange", Timezone = "Greenwich Standard Time" },
                    new Exchange { Name = "ISLAND", LongName = "INET", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "JAKSE", LongName = "Jakarta Stock Exchange", Timezone = "S.E. Asia Standard Time" },
                    new Exchange { Name = "JASDA", LongName = "Japan Securities Dealers Association", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "JOH", LongName = "Johannesburg Stock Exchange", Timezone = "South Africa Standard Time" },
                    new Exchange { Name = "JOHSE", LongName = "Johannesburg Stock Exchange", Timezone = "South Africa Standard Time" },
                    new Exchange { Name = "JSE", LongName = "Johannesburg Stock Exchange", Timezone = "South Africa Standard Time" },
                    new Exchange { Name = "KARSE", LongName = "Karachi Stock Exchange", Timezone = "West Asia Standard Time" },
                    new Exchange { Name = "KCBOT", LongName = "Kansas City Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "KCBT", LongName = "Kansas City Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "KLS", LongName = "Kuala Lumpur Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "KLSI", LongName = "Kuala Lumpur Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "KOQ", LongName = "KOSDAQ", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KOQI", LongName = "KOSDAQ", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KOR", LongName = "Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KOREA", LongName = "Seoul Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KORI", LongName = "Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KRX", LongName = "Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KSE", LongName = "Korea Stock Exchange", Timezone = "Korea Standard Time" },
                    new Exchange { Name = "KUALA", LongName = "Kuala Lumpur Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "KYOSE", LongName = "Kyoto Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "LCE", LongName = "London Commodity Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LIFE", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LIFFE", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LIFFE_NF", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LIMSE", LongName = "Lima Stock Exchange", Timezone = "S.A. Pacific Standard Time" },
                    new Exchange { Name = "LINC", LongName = "Data Broadcasting Corporation", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "LIS", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "LISSE", LongName = "Lisbon Stock Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LME", LongName = "London Metal Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LON", LongName = "LSE UK Level 1", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LSE", LongName = "London Stock Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LSIN", LongName = "LSE International Market Service", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LSO", LongName = "LIFFE short options add ons and delta factors", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LTO", LongName = "London Traded Options", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "LUX", LongName = "Luxembourg Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "LUXI", LongName = "Luxembourg Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "LUXSE", LongName = "Luxembourg Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "M", LongName = "Chicago (Midwest) Stock Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MAC", LongName = "Madrid Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MADSE", LongName = "Madrid Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MASE", LongName = "MASE Westpac (London Bullion Market)", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "MATF", LongName = "Matif", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MATIF", LongName = "Euronext France", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MATIFF", LongName = "MATIF Financial Futures", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MBFX", LongName = "Midland Bank Forex", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "MC", LongName = "Montreal Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MCEDB", LongName = "Milan CED Borsa", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MDE", LongName = "Madrid Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MDEI", LongName = "Madrid Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ME", LongName = "Montreal Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MEFF", LongName = "Spanish Financial Futures Market: Barcelona", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MEFFO", LongName = "Spanish Options Market: Madrid", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MEFFRV", LongName = "Spanish Futures & Options Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MEFFRY", LongName = "Mercando Ispaniol de Futuros Finansial", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MEX", LongName = "Mexico Stock Exchange", Timezone = "Mexico Standard Time" },
                    new Exchange { Name = "MEXI", LongName = "Mexico Stock Exchange", Timezone = "Mexico Standard Time" },
                    new Exchange { Name = "MEXSE", LongName = "Mexico Stock Exchange", Timezone = "Mexico Standard Time" },
                    new Exchange { Name = "MF", LongName = "Mutual Funds", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MFE", LongName = "Montreal Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MGE", LongName = "Minneapolis Grain Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MIC", LongName = "Bonneville Market Information Corp.", Timezone = "S.A. Pacific Standard Time" },
                    new Exchange { Name = "MICEX", LongName = "Moscow Interbank Currency Exchange", Timezone = "Russian Standard Time" },
                    new Exchange { Name = "MIDAM", LongName = "MidAmerica Commodity Exchange", Timezone = "Central America Standard Time" },
                    new Exchange { Name = "MIDWES", LongName = "Midwest Stock Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MM", LongName = "Money Market Fund", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MONE", LongName = "Mercado Espanol de Futuros Finacial", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MONEP", LongName = "Paris Options", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MSE", LongName = "Montreal Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MSEI", LongName = "Milan Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MSO", LongName = "Montreal Stock Options", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MSPT", LongName = "Milan Stock Pit trading and corporate bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MT", LongName = "Chicago Mercantile Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MTS", LongName = "Milan Telematico Stocks", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MUN", LongName = "Deutshe Borse Stocks Level 1", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MUNSE", LongName = "Munich Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "MVSE", LongName = "Montevideo Stock Exchange", Timezone = "S.A. Eastern Standard Time" },
                    new Exchange { Name = "MX", LongName = "Montreal Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "N", LongName = "New York Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASD", LongName = "NASDAQ", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDAQ", LongName = "NASDAQ National Market", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDC", LongName = "National Assoc. of Securities Dealers (Canada)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDSC", LongName = "NASDAQ Small Caps", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NAT", LongName = "Nat West Bank Forex", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "NB", LongName = "New York Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NGYSE", LongName = "Nagoya Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "NIGSE", LongName = "Niigata Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "NIKKEI", LongName = "Nikkei-Needs Database", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "NLK", LongName = "Amsterdam", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "NLX", LongName = "National Labor Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NO", LongName = "New York Stock Exchange (Options)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NOTCBF", LongName = "Nasdaq OTC Bulletin Board Service : Foreign Issues", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NPE", LongName = "OMX Nordpool", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "NQLX", LongName = "Nasdaq LIFFE Markets", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NSE", LongName = "National Stock Exchange of India", Timezone = "India Standard Time" },
                    new Exchange { Name = "NYBOT", LongName = "New York Board of Trade", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYCE", LongName = "New York Cotton Exchange(CEC)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYFE", LongName = "New York Futures Exchange(CEC)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYLCD", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "NYLID", LongName = "London Inter. Financial Futures Exchange", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "NYLUS", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "NYME", LongName = "New York Mercantile Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYMEX", LongName = "New York Mercantile Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYMI", LongName = "New York Mercantile Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYSE", LongName = "New York Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NYSELIFFE", LongName = "New York Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NZSE", LongName = "New Zealand Stock Exchange", Timezone = "New Zealand Standard Time" },
                    new Exchange { Name = "NZX", LongName = "New Zealand Stock Exchange", Timezone = "New Zealand Standard Time" },
                    new Exchange { Name = "NZXI", LongName = "New Zealand Stock Exchange", Timezone = "New Zealand Standard Time" },
                    new Exchange { Name = "OETOB", LongName = "Austrian Stock and Options Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OFX", LongName = "OFEX London", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "OM", LongName = "OMX Derivatives Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OMFE", LongName = "Oslo Opsjon", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OMS", LongName = "Stockholm Exchange - Derivatives Market", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "ONE", LongName = "OneChicago", Timezone = "Central Standard Time" },
                    new Exchange { Name = "OPRA", LongName = "Option Price Reporting Authority", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "OPTS", LongName = "Generic Options Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "OSASE", LongName = "Osaka Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "OSE", LongName = "Osaka Securities Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "OSE.JPN", LongName = "Osaka Securities Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "OSEI", LongName = "Osaka Securities Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "OSL", LongName = "Oslo Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OSLI", LongName = "Oslo Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OSLSE", LongName = "Oslo Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "OT", LongName = "Pink Sheets", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "OTC", LongName = "Over The Counter", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "OTCBB", LongName = "OTC Bulletin Board", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "P", LongName = "Pacific Stock Exchange", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "PACIFI", LongName = "Pacific Stock Exchange", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "PAR", LongName = "Euronext Equities", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "PASE", LongName = "Paris Stock Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "PBT", LongName = "Philadelphia Board of Trade", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "PF", LongName = "PetroFlash", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "PHLX", LongName = "Philadelphia Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "PHPSE", LongName = "Philippine Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "PO", LongName = "Pacific Stock Exchange (Options)", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "PRIMI", LongName = "Milan Option", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "PSE", LongName = "Pacific Exchange", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "PSOFT", LongName = "Paris Softs", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "Q", LongName = "NASDAQ NMS", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "RIOSE", LongName = "Rio de Janeiro Stock Exchange", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "RTS", LongName = "Russian Trading System", Timezone = "Russian Standard Time" },
                    new Exchange { Name = "S", LongName = "NASDAQ Small Cap", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "SANSE", LongName = "Santiago Stock Exchange", Timezone = "Pacific S.A. Standard Time" },
                    new Exchange { Name = "SAPSE", LongName = "Sapporo Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "SBF", LongName = "Euronext France", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SEAQ", LongName = "International (US Securities traded on London Exch", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "SEAQL2", LongName = "SEAQ International Level 2", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "SEAQTR", LongName = "SEAQ International Trades Data", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "SES", LongName = "Singapore Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SESI", LongName = "Singapore Stock Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SET", LongName = "Bangkok Stock Exchange of Thailand", Timezone = "S.E. Asia Standard Time" },
                    new Exchange { Name = "SETI", LongName = "Bangkok Stock Exchange of Thailand", Timezone = "S.E. Asia Standard Time" },
                    new Exchange { Name = "SFB", LongName = "Stockholm Exchange - Stock Market", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SFE", LongName = "Sydney Futures Exchange", Timezone = "AUS Eastern Standard Time" },
                    new Exchange { Name = "SGX", LongName = "Singapore Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SHANG", LongName = "Shanghai Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "SHENZ", LongName = "Shenzen Stock Exchange", Timezone = "China Standard Time" },
                    new Exchange { Name = "SICOVA", LongName = "Paris Bonds", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SIMEX", LongName = "Singapore International Monetary Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SIMX", LongName = "Singapore International Monetary Exchange", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SING", LongName = "Stock Exchange of Singapore", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "SMART", LongName = "Smart", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "SNFE", LongName = "Sydney Futures Exchange", Timezone = "AUS Eastern Standard Time" },
                    new Exchange { Name = "SOFET", LongName = "Soffex Swiss Futures Financial Exchange", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SOFFEX", LongName = "EUREX", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SPAIN", LongName = "Mercato Continue Espana", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SPCC", LongName = "SPC Combined", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SPE", LongName = "SPECTRON", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "SPECI", LongName = "Special", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "SPSE", LongName = "Sao Paulo Stock Exchange", Timezone = "E. South America Standard Time" },
                    new Exchange { Name = "SSE", LongName = "Stockholm Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SSEI", LongName = "Stockholm Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "STKSE", LongName = "Stockholm Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "STREET", LongName = "Street Software", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "STX", LongName = "STOXX Indices", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWB", LongName = "Stuttgart Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWE", LongName = "Swiss Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWEI", LongName = "Swiss Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWX", LongName = "Swiss Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "SWXI", LongName = "Swiss Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "T", LongName = "NASDAQ Listed Stocks", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TAISE", LongName = "Taipei Stock Exchange of Taiwan", Timezone = "Singapore Standard Time" },
                    new Exchange { Name = "TC", LongName = "Toronto Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TELSE", LongName = "Tel Aviv Stock Exchange", Timezone = "Israel Standard Time" },
                    new Exchange { Name = "TFE", LongName = "Toronto Futures Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TIFFE", LongName = "Tokyo International Financial Futures Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "TSE", LongName = "Toronto Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TSE.JPN", LongName = "Tokyo Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "TSEI", LongName = "Tokyo Stock Exchange", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "TSO", LongName = "Toronto Stock Options", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "TUL", LongName = "Tullett and Tokyo", Timezone = "Tokyo Standard Time" },
                    new Exchange { Name = "U", LongName = "NASDAQ Bulletin Board", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "UNDEF", LongName = "Undefined", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "USDA", LongName = "U.S. Department of Agriculture", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "USDC", LongName = "U.S. Department of Commerce", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "UT", LongName = "Chicago Board of Trade", Timezone = "Central Standard Time" },
                    new Exchange { Name = "V", LongName = "NASDAQ Bulletin Board (pink)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "VC", LongName = "TSX Venture Exchange (CDNX)", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "VIE", LongName = "Wiener Borse Vienna Stock Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "VIEI", LongName = "Wiener Borse Vienna Stock Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "VIESE", LongName = "Vienna Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "VIRTX", LongName = "VIRT-X", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "VSE", LongName = "Vancouver Stock Exchange", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "VSO", LongName = "Vancouver Stock Options", Timezone = "Pacific Standard Time" },
                    new Exchange { Name = "VWAP", LongName = "IB VWAP Dealing Network", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "W", LongName = "CBOE (Non options) (QQQ, SPY)", Timezone = "Central Standard Time" },
                    new Exchange { Name = "WAR", LongName = "Warsaw Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "WARI", LongName = "Warsaw Stock Exchange", Timezone = "Central European Standard Time" },
                    new Exchange { Name = "WBE", LongName = "Wiener Borse Vienna Stock Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "WBEI", LongName = "Wiener Borse Vienna Stock Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "WCE", LongName = "Winnipeg Commodity Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "WOLFF", LongName = "Rudolf Wolff (London Metals)", Timezone = "GMT Standard Time" },
                    new Exchange { Name = "X", LongName = "Philadelphia Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "XET", LongName = "Deutshe Borse Stocks Level 1", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "XETRA", LongName = "XETRA Exchange", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "XO", LongName = "Philadelphia Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "ZS", LongName = "Zurich Stocks", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "ZURIB", LongName = "Zurich Bonds", Timezone = "W. Europe Standard Time" },
                    new Exchange { Name = "VENTURE", LongName = "TSX Venture", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CHX", LongName = " Chicago Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "DRCTEDGE", LongName = "Direct Edge", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NSX", LongName = "National Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BEX", LongName = "NASDAQ OMX BX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CBSX", LongName = "CBOE Stock Exchange", Timezone = "Central Standard Time" },
                    new Exchange { Name = "BATS", LongName = "BATS", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "EDGEA", LongName = "Direct Edge", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CHIXEN", LongName = "CHI-X Europe Ltd Clearnet", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "BATEEN", LongName = "BATS Europe", Timezone = "Central Europe Standard Time" },
                    new Exchange { Name = "VALUE", LongName = "IB Value Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "LAVA", LongName = "LavaFlow ECN", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CSFBALGO", LongName = "", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "JEFFALGO", LongName = "", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "BYX", LongName = "BATS BYX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "PSX", LongName = "NASDAQ OMX PSX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "GEMINI", LongName = "ISE Gemini", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDAQBX", LongName = "NASDAQ OMX BX Options Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "NASDAQOM", LongName = "NASDAQ OMX", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "CBOE2", LongName = "CBOE C2", Timezone = "Central Standard Time" },
                    new Exchange { Name = "MIAX", LongName = "MIAX Options Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "A", LongName = "American Stock Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "EDGX", LongName = "Bats EDGX Options Exchange", Timezone = "Eastern Standard Time" },
                    new Exchange { Name = "MERCURY", LongName = "ISE Mercury", Timezone = "Eastern Standard Time" }
                };

            foreach (Exchange e in exchanges)
            {
                context.Exchanges.AddOrUpdate(x => x.Name, e);
            }
            #endregion

            #region countries

            foreach (Country c in Countries)
            {
                context.Countries.AddOrUpdate(x => x.Name, c);
            }

            #endregion

            #region currencies

            var currencies = new List<Currency>
            {
                new Currency { Name = "East Caribbean Dollar", Code = "XCD", NumericCode = 951 },
                new Currency { Name = "Argentine Peso", Code = "ARS", NumericCode = 032 },
                new Currency { Name = "Armenian Dram", Code = "AMD", NumericCode = 051 },
                new Currency { Name = "Aruban Florin", Code = "AWG", NumericCode = 533 },
                new Currency { Name = "Australian Dollar", Code = "AUD", NumericCode = 036 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Azerbaijanian Manat", Code = "AZN", NumericCode = 944 },
                new Currency { Name = "Bahamian Dollar", Code = "BSD", NumericCode = 044 },
                new Currency { Name = "Bahraini Dinar", Code = "BHD", NumericCode = 048 },
                new Currency { Name = "Taka", Code = "BDT", NumericCode = 050 },
                new Currency { Name = "Barbados Dollar", Code = "BBD", NumericCode = 052 },
                new Currency { Name = "Belarusian Ruble", Code = "BYN", NumericCode = 933 },
                new Currency { Name = "Belarusian Ruble", Code = "BYR", NumericCode = 974 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Belize Dollar", Code = "BZD", NumericCode = 084 },
                new Currency { Name = "CFA Franc BCEAO", Code = "XOF", NumericCode = 952 },
                new Currency { Name = "Bermudian Dollar", Code = "BMD", NumericCode = 060 },
                new Currency { Name = "Indian Rupee", Code = "INR", NumericCode = 356 },
                new Currency { Name = "Ngultrum", Code = "BTN", NumericCode = 064 },
                new Currency { Name = "Boliviano", Code = "BOB", NumericCode = 068 },
                new Currency { Name = "Mvdol", Code = "BOV", NumericCode = 984 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Convertible Mark", Code = "BAM", NumericCode = 977 },
                new Currency { Name = "Pula", Code = "BWP", NumericCode = 072 },
                new Currency { Name = "Norwegian Krone", Code = "NOK", NumericCode = 578 },
                new Currency { Name = "Brazilian Real", Code = "BRL", NumericCode = 986 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Brunei Dollar", Code = "BND", NumericCode = 096 },
                new Currency { Name = "Bulgarian Lev", Code = "BGN", NumericCode = 975 },
                new Currency { Name = "CFA Franc BCEAO", Code = "XOF", NumericCode = 952 },
                new Currency { Name = "Burundi Franc", Code = "BIF", NumericCode = 108 },
                new Currency { Name = "Cabo Verde Escudo", Code = "CVE", NumericCode = 132 },
                new Currency { Name = "Riel", Code = "KHR", NumericCode = 116 },
                new Currency { Name = "CFA Franc BEAC", Code = "XAF", NumericCode = 950 },
                new Currency { Name = "Canadian Dollar", Code = "CAD", NumericCode = 124 },
                new Currency { Name = "Cayman Islands Dollar", Code = "KYD", NumericCode = 136 },
                new Currency { Name = "CFA Franc BEAC", Code = "XAF", NumericCode = 950 },
                new Currency { Name = "CFA Franc BEAC", Code = "XAF", NumericCode = 950 },
                new Currency { Name = "Chilean Peso", Code = "CLP", NumericCode = 152 },
                new Currency { Name = "Unidad de Fomento", Code = "CLF", NumericCode = 990 },
                new Currency { Name = "Yuan Renminbi", Code = "CNY", NumericCode = 156 },
                new Currency { Name = "Australian Dollar", Code = "AUD", NumericCode = 036 },
                new Currency { Name = "Australian Dollar", Code = "AUD", NumericCode = 036 },
                new Currency { Name = "Colombian Peso", Code = "COP", NumericCode = 170 },
                new Currency { Name = "Unidad de Valor Real", Code = "COU", NumericCode = 970 },
                new Currency { Name = "Comoro Franc", Code = "KMF", NumericCode = 174 },
                new Currency { Name = "Congolese Franc", Code = "CDF", NumericCode = 976 },
                new Currency { Name = "CFA Franc BEAC", Code = "XAF", NumericCode = 950 },
                new Currency { Name = "New Zealand Dollar", Code = "NZD", NumericCode = 554 },
                new Currency { Name = "Costa Rican Colon", Code = "CRC", NumericCode = 188 },
                new Currency { Name = "CFA Franc BCEAO", Code = "XOF", NumericCode = 952 },
                new Currency { Name = "Kuna", Code = "HRK", NumericCode = 191 },
                new Currency { Name = "Cuban Peso", Code = "CUP", NumericCode = 192 },
                new Currency { Name = "Peso Convertible", Code = "CUC", NumericCode = 931 },
                new Currency { Name = "Netherlands Antillean Guilder", Code = "ANG", NumericCode = 532 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Czech Koruna", Code = "CZK", NumericCode = 203 },
                new Currency { Name = "Danish Krone", Code = "DKK", NumericCode = 208 },
                new Currency { Name = "Djibouti Franc", Code = "DJF", NumericCode = 262 },
                new Currency { Name = "East Caribbean Dollar", Code = "XCD", NumericCode = 951 },
                new Currency { Name = "Dominican Peso", Code = "DOP", NumericCode = 214 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Egyptian Pound", Code = "EGP", NumericCode = 818 },
                new Currency { Name = "El Salvador Colon", Code = "SVC", NumericCode = 222 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "CFA Franc BEAC", Code = "XAF", NumericCode = 950 },
                new Currency { Name = "Nakfa", Code = "ERN", NumericCode = 232 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Ethiopian Birr", Code = "ETB", NumericCode = 230 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Falkland Islands Pound", Code = "FKP", NumericCode = 238 },
                new Currency { Name = "Danish Krone", Code = "DKK", NumericCode = 208 },
                new Currency { Name = "Fiji Dollar", Code = "FJD", NumericCode = 242 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "CFP Franc", Code = "XPF", NumericCode = 953 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "CFA Franc BEAC", Code = "XAF", NumericCode = 950 },
                new Currency { Name = "Dalasi", Code = "GMD", NumericCode = 270 },
                new Currency { Name = "Lari", Code = "GEL", NumericCode = 981 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Ghana Cedi", Code = "GHS", NumericCode = 936 },
                new Currency { Name = "Gibraltar Pound", Code = "GIP", NumericCode = 292 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Danish Krone", Code = "DKK", NumericCode = 208 },
                new Currency { Name = "East Caribbean Dollar", Code = "XCD", NumericCode = 951 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Quetzal", Code = "GTQ", NumericCode = 320 },
                new Currency { Name = "Pound Sterling", Code = "GBP", NumericCode = 826 },
                new Currency { Name = "Guinea Franc", Code = "GNF", NumericCode = 324 },
                new Currency { Name = "CFA Franc BCEAO", Code = "XOF", NumericCode = 952 },
                new Currency { Name = "Guyana Dollar", Code = "GYD", NumericCode = 328 },
                new Currency { Name = "Gourde", Code = "HTG", NumericCode = 332 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Australian Dollar", Code = "AUD", NumericCode = 036 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Lempira", Code = "HNL", NumericCode = 340 },
                new Currency { Name = "Hong Kong Dollar", Code = "HKD", NumericCode = 344 },
                new Currency { Name = "Forint", Code = "HUF", NumericCode = 348 },
                new Currency { Name = "Iceland Krona", Code = "ISK", NumericCode = 352 },
                new Currency { Name = "Indian Rupee", Code = "INR", NumericCode = 356 },
                new Currency { Name = "Rupiah", Code = "IDR", NumericCode = 360 },
                new Currency { Name = "SDR (Special Drawing Right)", Code = "XDR", NumericCode = 960 },
                new Currency { Name = "Iranian Rial", Code = "IRR", NumericCode = 364 },
                new Currency { Name = "Iraqi Dinar", Code = "IQD", NumericCode = 368 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Pound Sterling", Code = "GBP", NumericCode = 826 },
                new Currency { Name = "New Israeli Sheqel", Code = "ILS", NumericCode = 376 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Jamaican Dollar", Code = "JMD", NumericCode = 388 },
                new Currency { Name = "Yen", Code = "JPY", NumericCode = 392 },
                new Currency { Name = "Pound Sterling", Code = "GBP", NumericCode = 826 },
                new Currency { Name = "Jordanian Dinar", Code = "JOD", NumericCode = 400 },
                new Currency { Name = "Tenge", Code = "KZT", NumericCode = 398 },
                new Currency { Name = "Kenyan Shilling", Code = "KES", NumericCode = 404 },
                new Currency { Name = "Australian Dollar", Code = "AUD", NumericCode = 036 },
                new Currency { Name = "North Korean Won", Code = "KPW", NumericCode = 408 },
                new Currency { Name = "Won", Code = "KRW", NumericCode = 410 },
                new Currency { Name = "Kuwaiti Dinar", Code = "KWD", NumericCode = 414 },
                new Currency { Name = "Som", Code = "KGS", NumericCode = 417 },
                new Currency { Name = "Kip", Code = "LAK", NumericCode = 418 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Lebanese Pound", Code = "LBP", NumericCode = 422 },
                new Currency { Name = "Loti", Code = "LSL", NumericCode = 426 },
                new Currency { Name = "Rand", Code = "ZAR", NumericCode = 710 },
                new Currency { Name = "Liberian Dollar", Code = "LRD", NumericCode = 430 },
                new Currency { Name = "Libyan Dinar", Code = "LYD", NumericCode = 434 },
                new Currency { Name = "Swiss Franc", Code = "CHF", NumericCode = 756 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Pataca", Code = "MOP", NumericCode = 446 },
                new Currency { Name = "Denar", Code = "MKD", NumericCode = 807 },
                new Currency { Name = "Malagasy Ariary", Code = "MGA", NumericCode = 969 },
                new Currency { Name = "Malawi Kwacha", Code = "MWK", NumericCode = 454 },
                new Currency { Name = "Malaysian Ringgit", Code = "MYR", NumericCode = 458 },
                new Currency { Name = "Rufiyaa", Code = "MVR", NumericCode = 462 },
                new Currency { Name = "CFA Franc BCEAO", Code = "XOF", NumericCode = 952 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Ouguiya", Code = "MRO", NumericCode = 478 },
                new Currency { Name = "Mauritius Rupee", Code = "MUR", NumericCode = 480 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "ADB Unit of Account", Code = "XUA", NumericCode = 965 },
                new Currency { Name = "Mexican Peso", Code = "MXN", NumericCode = 484 },
                new Currency { Name = "Mexican Unidad de Inversion (UDI)", Code = "MXV", NumericCode = 979 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Moldovan Leu", Code = "MDL", NumericCode = 498 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Tugrik", Code = "MNT", NumericCode = 496 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "East Caribbean Dollar", Code = "XCD", NumericCode = 951 },
                new Currency { Name = "Moroccan Dirham", Code = "MAD", NumericCode = 504 },
                new Currency { Name = "Mozambique Metical", Code = "MZN", NumericCode = 943 },
                new Currency { Name = "Kyat", Code = "MMK", NumericCode = 104 },
                new Currency { Name = "Namibia Dollar", Code = "NAD", NumericCode = 516 },
                new Currency { Name = "Rand", Code = "ZAR", NumericCode = 710 },
                new Currency { Name = "Australian Dollar", Code = "AUD", NumericCode = 036 },
                new Currency { Name = "Nepalese Rupee", Code = "NPR", NumericCode = 524 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "CFP Franc", Code = "XPF", NumericCode = 953 },
                new Currency { Name = "New Zealand Dollar", Code = "NZD", NumericCode = 554 },
                new Currency { Name = "Cordoba Oro", Code = "NIO", NumericCode = 558 },
                new Currency { Name = "CFA Franc BCEAO", Code = "XOF", NumericCode = 952 },
                new Currency { Name = "Naira", Code = "NGN", NumericCode = 566 },
                new Currency { Name = "New Zealand Dollar", Code = "NZD", NumericCode = 554 },
                new Currency { Name = "Australian Dollar", Code = "AUD", NumericCode = 036 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Norwegian Krone", Code = "NOK", NumericCode = 578 },
                new Currency { Name = "Rial Omani", Code = "OMR", NumericCode = 512 },
                new Currency { Name = "Pakistan Rupee", Code = "PKR", NumericCode = 586 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Balboa", Code = "PAB", NumericCode = 590 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Kina", Code = "PGK", NumericCode = 598 },
                new Currency { Name = "Guarani", Code = "PYG", NumericCode = 600 },
                new Currency { Name = "Sol", Code = "PEN", NumericCode = 604 },
                new Currency { Name = "Philippine Peso", Code = "PHP", NumericCode = 608 },
                new Currency { Name = "New Zealand Dollar", Code = "NZD", NumericCode = 554 },
                new Currency { Name = "Zloty", Code = "PLN", NumericCode = 985 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Qatari Rial", Code = "QAR", NumericCode = 634 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Romanian Leu", Code = "RON", NumericCode = 946 },
                new Currency { Name = "Russian Ruble", Code = "RUB", NumericCode = 643 },
                new Currency { Name = "Rwanda Franc", Code = "RWF", NumericCode = 646 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Saint Helena Pound", Code = "SHP", NumericCode = 654 },
                new Currency { Name = "East Caribbean Dollar", Code = "XCD", NumericCode = 951 },
                new Currency { Name = "East Caribbean Dollar", Code = "XCD", NumericCode = 951 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "East Caribbean Dollar", Code = "XCD", NumericCode = 951 },
                new Currency { Name = "Tala", Code = "WST", NumericCode = 882 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Dobra", Code = "STD", NumericCode = 678 },
                new Currency { Name = "Saudi Riyal", Code = "SAR", NumericCode = 682 },
                new Currency { Name = "CFA Franc BCEAO", Code = "XOF", NumericCode = 952 },
                new Currency { Name = "Serbian Dinar", Code = "RSD", NumericCode = 941 },
                new Currency { Name = "Seychelles Rupee", Code = "SCR", NumericCode = 690 },
                new Currency { Name = "Leone", Code = "SLL", NumericCode = 694 },
                new Currency { Name = "Singapore Dollar", Code = "SGD", NumericCode = 702 },
                new Currency { Name = "Netherlands Antillean Guilder", Code = "ANG", NumericCode = 532 },
                new Currency { Name = "Sucre", Code = "XSU", NumericCode = 994 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Solomon Islands Dollar", Code = "SBD", NumericCode = 090 },
                new Currency { Name = "Somali Shilling", Code = "SOS", NumericCode = 706 },
                new Currency { Name = "Rand", Code = "ZAR", NumericCode = 710 },
                new Currency { Name = "South Sudanese Pound", Code = "SSP", NumericCode = 728 },
                new Currency { Name = "Euro", Code = "EUR", NumericCode = 978 },
                new Currency { Name = "Sri Lanka Rupee", Code = "LKR", NumericCode = 144 },
                new Currency { Name = "Sudanese Pound", Code = "SDG", NumericCode = 938 },
                new Currency { Name = "Surinam Dollar", Code = "SRD", NumericCode = 968 },
                new Currency { Name = "Norwegian Krone", Code = "NOK", NumericCode = 578 },
                new Currency { Name = "Lilangeni", Code = "SZL", NumericCode = 748 },
                new Currency { Name = "Swedish Krona", Code = "SEK", NumericCode = 752 },
                new Currency { Name = "Swiss Franc", Code = "CHF", NumericCode = 756 },
                new Currency { Name = "WIR Euro", Code = "CHE", NumericCode = 947 },
                new Currency { Name = "WIR Franc", Code = "CHW", NumericCode = 948 },
                new Currency { Name = "Syrian Pound", Code = "SYP", NumericCode = 760 },
                new Currency { Name = "New Taiwan Dollar", Code = "TWD", NumericCode = 901 },
                new Currency { Name = "Somoni", Code = "TJS", NumericCode = 972 },
                new Currency { Name = "Tanzanian Shilling", Code = "TZS", NumericCode = 834 },
                new Currency { Name = "Baht", Code = "THB", NumericCode = 764 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "CFA Franc BCEAO", Code = "XOF", NumericCode = 952 },
                new Currency { Name = "New Zealand Dollar", Code = "NZD", NumericCode = 554 },
                new Currency { Name = "Pa’anga", Code = "TOP", NumericCode = 776 },
                new Currency { Name = "Trinidad and Tobago Dollar", Code = "TTD", NumericCode = 780 },
                new Currency { Name = "Tunisian Dinar", Code = "TND", NumericCode = 788 },
                new Currency { Name = "Turkish Lira", Code = "TRY", NumericCode = 949 },
                new Currency { Name = "Turkmenistan New Manat", Code = "TMT", NumericCode = 934 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "Australian Dollar", Code = "AUD", NumericCode = 036 },
                new Currency { Name = "Uganda Shilling", Code = "UGX", NumericCode = 800 },
                new Currency { Name = "Hryvnia", Code = "UAH", NumericCode = 980 },
                new Currency { Name = "UAE Dirham", Code = "AED", NumericCode = 784 },
                new Currency { Name = "Pound Sterling", Code = "GBP", NumericCode = 826 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "US Dollar (Next day)", Code = "USN", NumericCode = 997 },
                new Currency { Name = "Peso Uruguayo", Code = "UYU", NumericCode = 858 },
                new Currency { Name = "Uruguay Peso en Unidades Indexadas (URUIURUI)", Code = "UYI", NumericCode = 940 },
                new Currency { Name = "Uzbekistan Sum", Code = "UZS", NumericCode = 860 },
                new Currency { Name = "Vatu", Code = "VUV", NumericCode = 548 },
                new Currency { Name = "Bolívar", Code = "VEF", NumericCode = 937 },
                new Currency { Name = "Dong", Code = "VND", NumericCode = 704 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "US Dollar", Code = "USD", NumericCode = 840 },
                new Currency { Name = "CFP Franc", Code = "XPF", NumericCode = 953 },
                new Currency { Name = "Moroccan Dirham", Code = "MAD", NumericCode = 504 },
                new Currency { Name = "Yemeni Rial", Code = "YER", NumericCode = 886 },
                new Currency { Name = "Zambian Kwacha", Code = "ZMW", NumericCode = 967 },
                new Currency { Name = "Zimbabwe Dollar", Code = "ZWL", NumericCode = 932 },
                new Currency { Name = "Bond Markets Unit European Composite Unit (EURCO)", Code = "XBA", NumericCode = 955 },
                new Currency { Name = "Bond Markets Unit European Monetary Unit (E.M.U.-6)", Code = "XBB", NumericCode = 956 },
                new Currency { Name = "Bond Markets Unit European Unit of Account 9 (E.U.A.-9)", Code = "XBC", NumericCode = 957 },
                new Currency { Name = "Bond Markets Unit European Unit of Account 17 (E.U.A.-17)", Code = "XBD", NumericCode = 958 },
                new Currency { Name = "Codes specifically reserved for testing purposes", Code = "XTS", NumericCode = 963 },
                new Currency { Name = "The codes assigned for transactions where no currency is involved", Code = "XXX", NumericCode = 999 },
                new Currency { Name = "Gold", Code = "XAU", NumericCode = 959 },
                new Currency { Name = "Palladium", Code = "XPD", NumericCode = 964 },
                new Currency { Name = "Platinum", Code = "XPT", NumericCode = 962 },
                new Currency { Name = "Silver", Code = "XAG", NumericCode = 961 },
            };

            //there are duplicates in the ISO dataset, so grab only distinct entries
            currencies = currencies.Distinct((x, y) => x.Code == y.Code).ToList();

            foreach (Currency c in currencies)
            {
                context.Currencies.AddOrUpdate(x => x.Code, c);
            }

            #endregion

            context.SaveChanges();

            #region exchangesessions
            //TODO Add seed data on exchange sessions
            #endregion

            #region instruments

            var spy = new Instrument
            {
                Symbol = "SPY",
                Currency = "USD",
                Type = InstrumentType.Stock,
                UnderlyingSymbol = "SPY",
                Datasource = context.Datasources.First(x => x.Name == "Yahoo"),
                Name = "SPDR S&P 500 ETF Trust",
                Multiplier = 1,
                MinTick = 0.01m,
                Industry = "Funds",
                Category = "Equity Fund",
                Subcategory = "Growth&Income-Large Cap",
                Exchange = context.Exchanges.First(x => x.Name == "NYSE"),
                SessionsSource = SessionsSource.Template,
                SessionTemplateID = context.SessionTemplates.First(x => x.Name == "U.S. Equities RTH").ID,
            };
            spy.Sessions = new List<InstrumentSession>();

            foreach (TemplateSession t in context.TemplateSessions.Where(x => x.TemplateID == spy.SessionTemplateID))
            {
                spy.Sessions.Add(t.ToInstrumentSession());
            }

            context.Instruments.AddOrUpdate(x => new {x.Symbol, x.DatasourceID, x.Expiration, x.ExchangeID}, spy);
            #endregion

            context.SaveChanges();

            context.Dispose();
        }
    }
}
