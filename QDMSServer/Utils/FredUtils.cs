// -----------------------------------------------------------------------
// <copyright file="FredUtils.cs" company="">
// Copyright 2014 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using QDMS;

namespace QDMSServer
{
    public static class FredUtils
    {
        /// <summary>
        /// Convert a FRED series to a QDMS instrument.
        /// Datasource needs to be set externally to FRED.
        /// </summary>
        public static Instrument SeriesToInstrument(FredSeries series)
        {
            return new Instrument
            {
                Symbol = series.ID,
                DatasourceSymbol = series.ID,
                Name = series.Title,
                Multiplier = 1,
                Type = InstrumentType.Index
            };
        }

        public static string FrequencyToRequestString(BarSize frequency)
        {
            switch (frequency)
            {
                case BarSize.OneDay:
                    return "d";
                    
                case BarSize.OneWeek:
                    return "w";

                case BarSize.OneMonth:
                    return "m";

                case BarSize.OneQuarter:
                    return "q";

                case BarSize.OneYear:
                    return "a";

                default:
                    return "d";
            }
        }

        /// <summary>
        /// Searches Fred for instruments matching the string search parameter.
        /// </summary>
        /// <param name="search">Search string.</param>
        /// <param name="apiKey"></param>
        /// <returns>A list of instruments matching the search parameter.</returns>
        public async static Task<IEnumerable<FredSeries>> FindSeries(string search, string apiKey)
        {
            string url = string.Format("http://api.stlouisfed.org/fred/series/search?search_text={0}&api_key={1}",
                search,
                apiKey);

            string xml;

            using (var webClient = new WebClient())
            {
                //exceptions should be handled further up
                xml = await webClient.DownloadStringTaskAsync(url);
            }

            return ParseSeriesXML(xml);
        }

        //Format:
        //<seriess realtime_start="2013-08-14" realtime_end="2013-08-14" order_by="search_rank" sort_order="desc" count="25" offset="0" limit="1000">
        //
        //<series id="MSIM2" realtime_start="2013-08-14" realtime_end="2013-08-14" 
        //title="Monetary Services Index: M2 (preferred)" 
        //observation_start="1967-01-01" observation_end="2013-06-01" 
        //frequency="Monthly" frequency_short="M" units="Billions of Dollars" 
        //units_short="Bil. of $" seasonal_adjustment="Seasonally Adjusted" 
        //seasonal_adjustment_short="SA" last_updated="2013-07-12 11:01:06-05" 
        //popularity="52" 
        //notes="The MSI measure the flow of monetary services received each period by households and firms from their holdings of monetary assets (levels of the indexes are sometimes referred to as Divisia monetary aggregates). Preferred benchmark rate equals 100 basis points plus the largest rate in the set of rates. Alternative benchmark rate equals the larger of the preferred benchmark rate and the Baa corporate bond yield. More information about the new MSI can be found at http://research.stlouisfed.org/msi/index.html."/>
        private static IEnumerable<FredSeries> ParseSeriesXML(string xml)
        {
            var serializer = new XmlSerializer(typeof(FredSeries));
            XDocument xdoc;
            using(StringReader sr = new StringReader(xml))
            {
                 xdoc = XDocument.Load(sr);
            }

            return xdoc.Descendants("series").Select(x => (FredSeries)serializer.Deserialize(x.CreateReader()));
        }

        [Serializable]
        [XmlRoot("series")]
        public class FredSeries
        {
            [XmlAttribute("id")]
            public string ID { get; set; }

            [XmlAttribute("title")]
            public string Title { get; set; }

            [XmlAttribute("seasonal_adjustment")]
            public string SeasonalAdjustment { get; set; }

            [XmlAttribute("observation_start")]
            public string From { get; set; }

            [XmlAttribute("observation_end")]
            public string To { get; set; }

            [XmlAttribute("frequency")]
            public string Frequency { get; set; }

            [XmlAttribute("units_short")]
            public string Units { get; set; }

            [XmlAttribute("notes")]
            public string Notes { get; set; }
        }
    }
}
