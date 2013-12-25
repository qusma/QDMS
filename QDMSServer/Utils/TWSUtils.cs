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

        public static OHLCBar HistoricalDataEventArgsToOHLCBar(Krs.Ats.IBNet.HistoricalDataEventArgs e)
        {
            var bar = new OHLCBar
            {
                DT = e.Date,
                Open = e.Open,
                High = e.High,
                Low = e.Low,
                Close = e.Close,
                Volume = e.Volume,
                AdjOpen = e.Open,
                AdjHigh = e.High,
                AdjLow = e.Low,
                AdjClose = e.Close
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
                return Math.Ceiling(t.TotalDays / 365).ToString("0") + " Y";
            if(minFreq >= QDMS.BarSize.OneMonth)
                return Math.Ceiling(t.TotalDays / 29).ToString("0") + " M";
            if(minFreq >= QDMS.BarSize.OneWeek)
                return Math.Ceiling(t.TotalDays / 7).ToString("0") + " W";
            if (minFreq >= QDMS.BarSize.OneDay)
                return Math.Ceiling(t.TotalDays).ToString("0") + " D";

            return Math.Ceiling(t.TotalSeconds).ToString("0") + " S";
        }

        public static Krs.Ats.IBNet.BarSize BarSizeConverter(QDMS.BarSize freq)
        {
            if (freq == QDMS.BarSize.Tick) throw new Exception("Bar size conversion impossible, TWS does not suppor tick size");
            return (Krs.Ats.IBNet.BarSize)(int)freq;
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

            if (!string.IsNullOrEmpty(contract.Summary.PrimaryExchange))
                instrument.PrimaryExchange = new Exchange { Name = contract.Summary.PrimaryExchange };
            return instrument;
        }

        public static Contract InstrumentToContract(Instrument instrument)
        {
            string symbol = string.IsNullOrEmpty(instrument.DatasourceSymbol) ? instrument.Symbol : instrument.DatasourceSymbol;
            var contract = new Contract(
                0,
                symbol,
                SecurityTypeConverter(instrument.Type),
                instrument.Expiration.HasValue ? instrument.Expiration.Value.ToString("yyyyMM", CultureInfo.InvariantCulture) : "",
                0,
                OptionTypeToRightType(instrument.OptionType),
                instrument.Multiplier.ToString(),
                "",
                instrument.Currency,
                null,
                null,
                SecurityIdType.None,
                string.Empty);

            //if it's a future, the symbol isn't actually the symbol but the underlying
            //TODO pretty sure this needs to be done for other contract types as well?
            if (instrument.Type == InstrumentType.Future)
            {
                contract.Symbol = instrument.UnderlyingSymbol;
            }

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
                "",
                e.Time,
                e.Open,
                e.High,
                e.Low,
                e.Close,
                e.Volume,
                e.Wap,
                e.Count);
        }
    }
}
