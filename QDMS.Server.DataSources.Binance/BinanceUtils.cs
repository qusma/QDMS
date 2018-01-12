// -----------------------------------------------------------------------
// <copyright file="BinanceUtils.cs" company="">
// Copyright 2018 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace QDMS.Server.DataSources.Binance
{
    public static class BinanceUtils
    {
        public static async Task<List<Instrument>> GetInstruments(Datasource binance)
        {
            var instruments = new List<Instrument>();
            var client = new HttpClient();
            var allPrices = await client.GetAsync("https://www.binance.com/api/v1/ticker/allPrices");
            allPrices.EnsureSuccessStatusCode();

            var array = JArray.Parse(await allPrices.Content.ReadAsStringAsync());
            foreach (var item in array)
            {
                var inst = new Instrument()
                {
                    Symbol = item["symbol"].ToString(),
                    Name = item["symbol"].ToString(),
                    Datasource = binance,
                    DatasourceID = binance.ID,
                    Multiplier = 1,
                    MinTick = 0.00000001m,
                    Type = InstrumentType.CryptoCurrency
                };
                instruments.Add(inst);
            }

            return instruments;
        }
    }
}