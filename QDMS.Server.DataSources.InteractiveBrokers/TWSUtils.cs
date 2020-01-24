// -----------------------------------------------------------------------
// <copyright file="TWSUtils.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using QDMSIBClient;
using IBApi;
using QDMS;
using QDMS.Utils;

namespace QDMSServer
{
    public static class TWSUtils
    {
        public static ErrorArgs ConvertErrorArguments(ErrorEventArgs args)
        {
            return new ErrorArgs((int)args.ErrorCode, args.ErrorMsg);
        }

        /// <summary>
        /// Check if a request for historical data obeys the duration limits of TWS.
        /// </summary>
        /// <returns>True if the request obeys the limits, false otherwise.</returns>
        public static bool RequestObeysLimits(HistoricalDataRequest request)
        {
            //The limitations are laid out here: https://www.interactivebrokers.com/en/software/api/apiguide/tables/historical_data_limitations.htm
            TimeSpan period = (request.EndingDate - request.StartingDate);
            double periodSeconds = period.TotalSeconds;
            double freqSeconds = request.Frequency.ToTimeSpan().TotalSeconds;

            //if (periodSeconds / freqSeconds > 2000) return false; //what was the purpose of this?

            return periodSeconds < MaxRequestLength(request.Frequency);
        }

        /// <summary>
        /// Returns the maximum period length of a historical data request, in seconds, depending on the data frequency.
        /// </summary>
        /// <param name="frequency"></param>
        /// <returns>Maximum allowed length in </returns>
        public static int MaxRequestLength(QDMS.BarSize frequency)
        {
            //The limitations are laid out here: https://www.interactivebrokers.com/en/software/api/apiguide/tables/historical_data_limitations.htm
            if (frequency <= QDMS.BarSize.OneSecond)      return 1800;
            if (frequency <= QDMS.BarSize.FiveSeconds)    return 7200;
            if (frequency <= QDMS.BarSize.FifteenSeconds) return 14400;
            if (frequency <= QDMS.BarSize.ThirtySeconds)  return 24 * 3600;
            if (frequency <= QDMS.BarSize.OneMinute)      return 2 * 24 * 3600;
            if (frequency <= QDMS.BarSize.ThirtyMinutes)  return 7 * 24 * 3600;
            if (frequency <= QDMS.BarSize.OneHour)        return 29 * 24 * 3600;
            return 365 * 24 * 3600;
        }

        public static OHLCBar HistoricalDataEventArgsToOHLCBar(QDMSIBClient.HistoricalDataEventArgs e)
        {
            var bar = new OHLCBar
            {
                DTOpen = e.Bar.Time,
                Open = (decimal)e.Bar.Open,
                High = (decimal)e.Bar.High,
                Low = (decimal)e.Bar.Low,
                Close = (decimal)e.Bar.Close,
                Volume = e.Bar.Volume,
            };
            return bar;
        }

        public static RealTimeDataEventArgs HistoricalDataEventArgsToRealTimeDataEventArgs(QDMSIBClient.HistoricalDataEventArgs e, int instrumentId, int reqId)
        {
            var rtdea = new RealTimeDataEventArgs(instrumentId,
                QDMS.BarSize.FiveSeconds,
                MyUtils.ConvertToTimestamp(e.Bar.Time),
                (decimal)e.Bar.Open,
                (decimal)e.Bar.High,
                (decimal)e.Bar.Low,
                (decimal)e.Bar.Close,
                e.Bar.Volume,
                e.Bar.WAP,
                e.Bar.Count,
                reqId);
            return rtdea;
        }

        public static string TimespanToDurationString(TimeSpan t, QDMS.BarSize minFreq)
        {
            //   duration:
            //     This is the time span the request will cover, and is specified using the
            //     format: , i.e., 1 D, where valid units are: S (seconds) D (days) W (weeks)
            //     M (months) Y (years) If no unit is specified, seconds are used. "years" is
            //     currently limited to one.
            if (minFreq > QDMS.BarSize.OneMonth)
                return Math.Ceiling(Math.Max(1, t.TotalDays / 365)).ToString("0") + " Y";
            if(minFreq >= QDMS.BarSize.OneMonth)
                return Math.Ceiling(Math.Max(1, t.TotalDays / 29)).ToString("0") + " M";
            if(minFreq >= QDMS.BarSize.OneWeek)
                return Math.Ceiling(Math.Max(1, t.TotalDays / 7)).ToString("0") + " W";
            if (minFreq >= QDMS.BarSize.OneDay || t.TotalSeconds > 86400)
            {
                if (t.TotalDays > 14)
                {
                    //This is a ridiculous hack made necessary by the incredibly bad TWS API
                    //For longer periods, if we specify the period as a # of days, the request is rejected!
                    //so instead we do it as the number of weeks and everything is A-OK
                    return Math.Ceiling(t.TotalDays / 7).ToString("0") + " W";
                }
                else
                {
                    return Math.Ceiling(Math.Max(1, t.TotalDays)).ToString("0") + " D";
                }
            }

            return Math.Ceiling(t.TotalSeconds).ToString("0") + " S";
        }

        public static QDMSIBClient.BarSize BarSizeConverter(QDMS.BarSize freq)
        {
                switch (freq)
                {
                    case QDMS.BarSize.Tick:
                        throw new Exception("Bar size conversion impossible, TWS does not suppor tick BarSize");
                	case QDMS.BarSize.OneSecond:
                        return QDMSIBClient.BarSize.OneSecond;
                    case QDMS.BarSize.FiveSeconds:
                        return QDMSIBClient.BarSize.FiveSeconds;
                    case QDMS.BarSize.FifteenSeconds:
                        return QDMSIBClient.BarSize.FifteenSeconds;
                    case QDMS.BarSize.ThirtySeconds:
                        return QDMSIBClient.BarSize.ThirtySeconds;
                    case QDMS.BarSize.OneMinute:
                        return QDMSIBClient.BarSize.OneMinute;
                    case QDMS.BarSize.TwoMinutes:
                        return QDMSIBClient.BarSize.TwoMinutes;
                    case QDMS.BarSize.FiveMinutes:
                        return QDMSIBClient.BarSize.FiveMinutes;
                    case QDMS.BarSize.FifteenMinutes:
                        return QDMSIBClient.BarSize.FifteenMinutes;
                    case QDMS.BarSize.ThirtyMinutes:
                        return QDMSIBClient.BarSize.ThirtyMinutes;
                    case QDMS.BarSize.OneHour:
                        return QDMSIBClient.BarSize.OneHour;
                    case QDMS.BarSize.OneDay:
                        return QDMSIBClient.BarSize.OneDay;
                    case QDMS.BarSize.OneWeek:
                        return QDMSIBClient.BarSize.OneWeek;
                    case QDMS.BarSize.OneMonth:
                        return QDMSIBClient.BarSize.OneMonth;
                    case QDMS.BarSize.OneQuarter:
                        throw new Exception("Bar size conversion impossible, TWS does not suppor quarter BarSize.");
                    case QDMS.BarSize.OneYear:
                        return QDMSIBClient.BarSize.OneYear;

                    default:
                        return QDMSIBClient.BarSize.OneDay;
                }
        }

        public static QDMS.BarSize BarSizeConverter(QDMSIBClient.BarSize freq)
        {
            if (freq == QDMSIBClient.BarSize.OneYear) return QDMS.BarSize.OneYear;
            return (QDMS.BarSize)(int)freq;
        }

        public static string OptionTypeToRightType(OptionType? type)
        {
            if (type == OptionType.Put) return "PUT";
            if (type == OptionType.Call) return "CALL";
            return "";
        }

        public static OptionType? RightTypeToOptionType(string right)
        {
            if (right == "P" || right == "PUT") return OptionType.Put;
            if (right == "C" || right == "CALL") return OptionType.Call;
            return null;
        }

        public static string SecurityTypeConverter(InstrumentType type)
        {
            if((int)type >= 13)
            {
                throw new Exception(string.Format("Can not convert InstrumentType {0} to SecurityType", type));
            }
            return EnumDescConverter.GetEnumDescription(type);
        }

        public static InstrumentType InstrumentTypeConverter(string type)
        {
            return (InstrumentType)EnumDescConverter.GetEnumValue(typeof(InstrumentType), type);
        }


        public static Instrument ContractDetailsToInstrument(ContractDetails contractDetails)
        {
            var instrument = new Instrument
            {
                Symbol = contractDetails.Contract.LocalSymbol,
                UnderlyingSymbol = contractDetails.Contract.Symbol,
                Name = contractDetails.LongName,
                OptionType = RightTypeToOptionType(contractDetails.Contract.Right),
                Type = InstrumentTypeConverter(contractDetails.Contract.SecType),
                Multiplier = contractDetails.Contract.Multiplier == null ? 1 : int.Parse(contractDetails.Contract.Multiplier),
                Expiration = ConvertExpiration(contractDetails),
                Strike = (decimal)contractDetails.Contract.Strike,
                Currency = contractDetails.Contract.Currency,
                MinTick = (decimal)contractDetails.MinTick,
                Industry = contractDetails.Industry,
                Category = contractDetails.Category,
                Subcategory = contractDetails.Subcategory,
                IsContinuousFuture = false,
                ValidExchanges = contractDetails.ValidExchanges
            };

            if (instrument.Type == InstrumentType.Future ||
                instrument.Type == InstrumentType.FutureOption ||
                instrument.Type == InstrumentType.Option)
            {
                instrument.TradingClass = contractDetails.Contract.TradingClass;
            }

            if (!string.IsNullOrEmpty(contractDetails.Contract.PrimaryExch))
                instrument.PrimaryExchange = new Exchange { Name = contractDetails.Contract.PrimaryExch };
            return instrument;
        }

        private static DateTime? ConvertExpiration(ContractDetails contractDetails)
        {
            return string.IsNullOrEmpty(contractDetails.Contract.LastTradeDateOrContractMonth) 
                ? (DateTime?)null 
                : DateTime.ParseExact(contractDetails.Contract.LastTradeDateOrContractMonth, "yyyyMMdd", CultureInfo.InvariantCulture);
            //todo is this sometimes yyyyMM for options/futures "contract month"?
        }

        public static Contract InstrumentToContract(Instrument instrument)
        {
            string symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.UnderlyingSymbol : instrument.DatasourceSymbol;
            string expirationString = "";

            //multiple options expire each month so the string needs to be more specific there
            if(instrument.Expiration.HasValue)
            {
                expirationString = instrument.Expiration.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            }


            var contract = new Contract()
            {
                Symbol = symbol,
                SecType = SecurityTypeConverter(instrument.Type),
                LastTradeDateOrContractMonth = expirationString,
                Right = OptionTypeToRightType(instrument.OptionType),
                Multiplier = instrument.Multiplier.ToString(),
                Currency = instrument.Currency,
                PrimaryExch = instrument.PrimaryExchange == null ? null : instrument.PrimaryExchange.Name,
                SecIdType = "None",
                Exchange = "",
                SecId = ""
            };

            contract.TradingClass = instrument.TradingClass;

            contract.IncludeExpired = instrument.Expiration.HasValue; //only set IncludeExpired to true if the contract can actually expire

            if (instrument.Strike.HasValue && instrument.Strike.Value != 0)
                contract.Strike = (double)instrument.Strike.Value;

            if (instrument.Exchange != null)
                contract.Exchange = instrument.Exchange.Name;

            return contract;
        }

        /// <summary>
        /// Returns RealTimeDataEventArgs derived from IB's  RealTimeBarEventArgs, but not including the symbol
        /// </summary>
        /// <param name="e">RealTimeBarEventArgs</param>
        /// <returns>RealTimeDataEventArgs </returns>
        public static RealTimeDataEventArgs RealTimeDataEventArgsConverter(RealTimeBarEventArgs e, QDMS.BarSize frequency)
        {
            return new RealTimeDataEventArgs(
                0,
                frequency,
                e.Time,
                (decimal)e.Open,
                (decimal)e.High,
                (decimal)e.Low,
                (decimal)e.Close,
                e.Volume,
                e.Wap,
                e.Count,
                0);
        }
    }
}
