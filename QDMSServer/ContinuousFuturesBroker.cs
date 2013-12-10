// -----------------------------------------------------------------------
// <copyright file="ContinuousFuturesBroker.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;

namespace QDMSServer
{
    public class ContinuousFuturesBroker
    {
        public void RequestHistoricalData(HistoricalDataRequest request)
        {
            var searchInstrument = new Instrument { UnderlyingSymbol = request.Instrument.UnderlyingSymbol, Type = InstrumentType.Future };
            var futures = InstrumentManager.FindInstruments(search: searchInstrument);


        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="continuousFuture"></param>
        /// <returns>The current front future for this continuous futures contract. Null if it is not found.</returns>
        public Instrument GetCurrentFrontFuture(Instrument continuousFuture)
        {
            var searchInstrument = new Instrument { UnderlyingSymbol = continuousFuture.UnderlyingSymbol, Type = InstrumentType.Future };
            var futures = InstrumentManager.FindInstruments(search: searchInstrument);

            if (futures.Count == 0) return null;

            return new Instrument();
        }
    }
}
