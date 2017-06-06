// -----------------------------------------------------------------------
// <copyright file="TWSUtils.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using Krs.Ats.IBNet;
using QDMS;

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

        public static OHLCBar HistoricalDataEventArgsToOHLCBar(Krs.Ats.IBNet.HistoricalDataEventArgs e)
        {
            var bar = new OHLCBar
            {
                DTOpen = e.Date,
                Open = e.Open,
                High = e.High,
                Low = e.Low,
                Close = e.Close,
                Volume = e.Volume,
            };
            return bar;
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

        public static Krs.Ats.IBNet.BarSize BarSizeConverter(QDMS.BarSize freq)
        {
                switch (freq)
                {
                    case QDMS.BarSize.Tick:
                        throw new Exception("Bar size conversion impossible, TWS does not suppor tick BarSize");
                	case QDMS.BarSize.OneSecond:
                        return Krs.Ats.IBNet.BarSize.OneSecond;
                    case QDMS.BarSize.FiveSeconds:
                        return Krs.Ats.IBNet.BarSize.FiveSeconds;
                    case QDMS.BarSize.FifteenSeconds:
                        return Krs.Ats.IBNet.BarSize.FifteenSeconds;
                    case QDMS.BarSize.ThirtySeconds:
                        return Krs.Ats.IBNet.BarSize.ThirtySeconds;
                    case QDMS.BarSize.OneMinute:
                        return Krs.Ats.IBNet.BarSize.OneMinute;
                    case QDMS.BarSize.TwoMinutes:
                        return Krs.Ats.IBNet.BarSize.TwoMinutes;
                    case QDMS.BarSize.FiveMinutes:
                        return Krs.Ats.IBNet.BarSize.FiveMinutes;
                    case QDMS.BarSize.FifteenMinutes:
                        return Krs.Ats.IBNet.BarSize.FifteenMinutes;
                    case QDMS.BarSize.ThirtyMinutes:
                        return Krs.Ats.IBNet.BarSize.ThirtyMinutes;
                    case QDMS.BarSize.OneHour:
                        return Krs.Ats.IBNet.BarSize.OneHour;
                    case QDMS.BarSize.OneDay:
                        return Krs.Ats.IBNet.BarSize.OneDay;
                    case QDMS.BarSize.OneWeek:
                        return Krs.Ats.IBNet.BarSize.OneWeek;
                    case QDMS.BarSize.OneMonth:
                        return Krs.Ats.IBNet.BarSize.OneMonth;
                    case QDMS.BarSize.OneQuarter:
                        throw new Exception("Bar size conversion impossible, TWS does not suppor quarter BarSize.");
                    case QDMS.BarSize.OneYear:
                        return Krs.Ats.IBNet.BarSize.OneYear;

                    default:
                        return Krs.Ats.IBNet.BarSize.OneDay;
                }
        }

        public static QDMS.BarSize BarSizeConverter(Krs.Ats.IBNet.BarSize freq)
        {
            if (freq == Krs.Ats.IBNet.BarSize.OneYear) return QDMS.BarSize.OneYear;
            return (QDMS.BarSize)(int)freq;
        }

        public static RightType OptionTypeToRightType(OptionType? type)
        {
            if (type == null) return RightType.Undefined;
            if (type == OptionType.Call) return RightType.Call;
            return RightType.Put;
        }

        public static OptionType? RightTypeToOptionType(RightType right)
        {
            if (right == RightType.Undefined) return null;
            if (right == RightType.Call) return OptionType.Call;
            return OptionType.Put;
        }

        public static SecurityType SecurityTypeConverter(InstrumentType type)
        {
            if((int)type >= 13)
            {
                throw new Exception(string.Format("Can not convert InstrumentType {0} to SecurityType", type));
            }
            return (SecurityType)(int)type;
        }

        public static InstrumentType InstrumentTypeConverter(SecurityType type)
        {
            return (InstrumentType)(int)type;
        }


        public static Instrument ContractDetailsToInstrument(ContractDetails contract)
        {
            var instrument =  new Instrument
            {
                Symbol = contract.Summary.LocalSymbol,
                UnderlyingSymbol = contract.Summary.Symbol,
                Name = contract.LongName,
                OptionType = RightTypeToOptionType(contract.Summary.Right),
                Type = InstrumentTypeConverter(contract.Summary.SecurityType),
                Multiplier = contract.Summary.Multiplier == null ? 1 : int.Parse(contract.Summary.Multiplier),
                Expiration = string.IsNullOrEmpty(contract.Summary.Expiry) ? (DateTime?)null : DateTime.ParseExact(contract.Summary.Expiry, "yyyyMMdd", CultureInfo.InvariantCulture),
                Strike = (decimal)contract.Summary.Strike,
                Currency = contract.Summary.Currency,
                MinTick = (decimal)contract.MinTick,
                Industry = contract.Industry,
                Category = contract.Category,
                Subcategory = contract.Subcategory,
                IsContinuousFuture = false,
                ValidExchanges = contract.ValidExchanges
            };

            if (instrument.Type == InstrumentType.Future || 
                instrument.Type == InstrumentType.FutureOption ||
                instrument.Type == InstrumentType.Option)
            {
                instrument.TradingClass = contract.Summary.TradingClass;
            }

            if (!string.IsNullOrEmpty(contract.Summary.PrimaryExchange))
                instrument.PrimaryExchange = new Exchange { Name = contract.Summary.PrimaryExchange };
            return instrument;
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


            var contract = new Contract(
                0,
                symbol,
                SecurityTypeConverter(instrument.Type),
                expirationString,
                0,
                OptionTypeToRightType(instrument.OptionType),
                instrument.Multiplier.ToString(),
                "",
                instrument.Currency,
                instrument.Symbol,
                instrument.PrimaryExchange == null ? null : instrument.PrimaryExchange.Name,
                SecurityIdType.None,
                string.Empty);

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
        public static RealTimeDataEventArgs RealTimeDataEventArgsConverter(RealTimeBarEventArgs e)
        {
            return new RealTimeDataEventArgs(
                0,
                e.Time,
                e.Open,
                e.High,
                e.Low,
                e.Close,
                e.Volume,
                e.Wap,
                e.Count,
                0);
        }
    }
}
