// -----------------------------------------------------------------------
// <copyright file="QuandlUtils.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Xml.Linq;
using System.Linq;
using System.Threading.Tasks;
using QDMS;

namespace QDMSServer
{
    public static class QuandlUtils
    {
        /// <summary>
        /// Takes XML formated data from Quandl and turns it into a List of OHLCBars.
        /// </summary>
        public static List<OHLCBar> ParseXML(string data)
        {
            var doc = XDocument.Parse(data);
            var bars = new List<OHLCBar>();

            //standard checks that the XML is what it should be
            if (doc.Root == null)
            {
                throw new Exception("XML Parse error: no root.");
            }

            if (doc.Root.Element("column-names") == null)
            {
                throw new Exception("Quandl: Column names element not found.");
            }

            //this simply gives us a list of column names, needed to parse the data correctly later on
            List<string> columns = doc.Root.Element("column-names").Elements("column-name").Select(x => x.Value).ToList();

            //some columns are required..
            if (!columns.Contains("Date"))
            {
                throw new Exception("Quandl: no date column, cannot parse data.");
            }

            var dataElement = doc.Root.Element("data");
            if (dataElement == null)
            {
                throw new Exception("Quandl: No data present in XML file.");
            }

            bool skipBar, dateSet;
            //finally loop through each bar and try to parse it
            foreach (var barElements in dataElement.Elements("datum"))
            {
                skipBar = false;
                dateSet = false;
                decimal parsedValue;

                var bar = new OHLCBar();
                int counter = 0;
                foreach (var price in barElements.Elements("datum"))
                {
                    bool isNull = price.Attribute("nil") != null; //if the attribute "nil" exists, then the value of this field is null

                    switch (columns[counter])
                    {
                        case "Date":
                            if (isNull)
                            {
                                skipBar = true;
                                break;
                            }

                            bar.DT = DateTime.ParseExact(price.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                            dateSet = true;
                            break;

                        case "Open":
                            if (decimal.TryParse(price.Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedValue))
                                bar.Open = parsedValue;
                            break;

                        case "High":
                            if (decimal.TryParse(price.Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedValue))
                                bar.High = parsedValue;
                            break;

                        case "Low":
                            if (decimal.TryParse(price.Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedValue))
                                bar.Low = parsedValue;
                            break;

                        case "Close":
                            if (decimal.TryParse(price.Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedValue))
                                bar.Close = parsedValue;
                            break;

                        case "Settle": //some futures data series have "settle" field instead of "close"
                            if (!isNull && decimal.TryParse(price.Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedValue))
                                bar.Close = parsedValue;
                            break;

                            //volume and OI are not represented as ints for some reason
                        case "Volume":
                            double volume;
                            if (!isNull && double.TryParse(price.Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out volume))
                                bar.Volume = (long)volume;
                            break;

                        case "Open Interest":
                            double openInterest;
                            if (!isNull && double.TryParse(price.Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out openInterest))
                                bar.OpenInterest = (int)openInterest;
                            break;

                        case "Adjusted Close":
                            if (decimal.TryParse(price.Value, NumberStyles.Number | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out parsedValue))
                                bar.AdjClose = parsedValue;
                            break;
                    }

                    counter++;
                }

                //if the adjusted close is set, generate adjusted OHL
                if (bar.AdjClose.HasValue)
                {
                    decimal adjFactor = bar.AdjClose.Value / bar.Close;
                    bar.AdjOpen = bar.Open * adjFactor;
                    bar.AdjHigh = bar.High * adjFactor;
                    bar.AdjLow = bar.Low * adjFactor;
                }

                //make sure that the date has been set
                if (!dateSet)
                    skipBar = true;

                if (!skipBar)
                    bars.Add(bar);
            }

            return bars;
        }

        /// <summary>
        /// Searches Quandl for instruments matching the string search parameter.
        /// </summary>
        /// <param name="search">Search string.</param>
        /// <param name="authToken"></param>
        /// <param name="page">The "page" of results to download.</param>
        /// <returns>A list of instruments matching the search parameter.</returns>
        public static async Task<QuandlInstrumentSearchResult> FindInstruments(string search, string authToken, int page = 1)
        {
            page = Math.Max(1, page);
            string url = string.Format("http://www.quandl.com/api/v1/datasets.xml?query={0}&page={1}&auth_token={2}",
                search,
                page,
                authToken);

            string xml;

            using (var webClient = new WebClient())
            {
                //exceptions should be handled further up
                xml = await webClient.DownloadStringTaskAsync(url).ConfigureAwait(false);
            }

            int count;
            var instruments = ParseInstrumentXML(xml, out count);
            return new QuandlInstrumentSearchResult(instruments, count);
        }

        public class QuandlInstrumentSearchResult
        {
            public QuandlInstrumentSearchResult(List<Instrument> instruments, int count)
            {
                Instruments = instruments;
                Count = count;
            }
            public List<Instrument> Instruments;
            public int Count;
        }


        //XML Format:

        //<datasets>
        //<total-count type="integer">232701</total-count>
        //<current-page type="integer">2</current-page>
        //<per-page type="integer">20</per-page>
        //<docs type="array">
        //<doc>
        //<id type="integer">2316351</id>
        //<source-code>DOE</source-code>
        //<code>EIA_TOTALOILSUPPLY_A_OMAN127</code>
        //<name>Total Oil Supply: Oman</name>
        //<urlize-name>Total-Oil-Supply-Oman</urlize-name>
        //<description>
        //Units=Thousand Barrels Per Day. The U.S. Energy Information Administration (EIA) collects, analyzes, and disseminates independent and impartial energy information to promote sound policymaking, efficient markets, and public understanding of energy and its interaction with the economy and the environment. EIA provides a wide range of information and data products covering energy production, stocks, demand, imports, exports, and prices; and prepares analyses and special reports on topics of current interest.
        //</description>
        //<updated-at>2013-11-29T15:49:08Z</updated-at>
        //<frequency>annual</frequency>
        //<from-date>2007-12-31</from-date>
        //<to-date>2011-12-31</to-date>
        //<column-names type="array">
        //<column-name>Year</column-name>
        //<column-name>Thousand Barrels Per Day</column-name>
        //</column-names>
        //<private type="boolean">false</private>
        //<type nil="true" />
        //<display-url>
        //http://www.eia.gov/cfapps/ipdbproject/XMLinclude_3.cfm?tid=5&pid=53&pdid=53,55,57,58,59,56,54,62,63,64,65,66,67,68&aid=1&cid=regions&titleStr=Total%20Oil%20Supply%20(Thousand%20Barrels%20Per%20Day)&syid=2007&eyid=2011&form=&defaultid=3&typeOfUnit=STDUNIT&unit=TBPD&products=
        //</display-url>
        //</doc>
        public static List<Instrument> ParseInstrumentXML(string xml, out int count)
        {
            var instruments = new List<Instrument>();

            var doc = XDocument.Parse(xml);
            if (doc.Root == null) throw new Exception("Could not parse instrument XML: root null.");
            if (doc.Root.Element("docs") == null) throw new Exception("Could not parse instrument XML: root null.");

            if (doc.Root.Element("total-count") != null)
            {
                int.TryParse(doc.Root.Element("total-count").Value, out count);
            }
            else
            {
                count = 0;
            }

            foreach (var instXML in doc.Root.Element("docs").Elements("doc"))
            {
                var inst = new Instrument();

                if (instXML.Element("code") != null)
                    inst.Symbol = instXML.Element("code").Value;
                else 
                    continue;

                if (instXML.Element("source-code") != null)
                    inst.DatasourceSymbol = string.Format("{0}/{1}",
                        instXML.Element("source-code").Value,
                        instXML.Element("code").Value);
                else 
                    continue;

                if(instXML.Element("name") != null)
                    inst.Name = instXML.Element("name").Value;

                //we only want to return that types of instruments that we currently have the ability to parse..
                List<string> columnNames = instXML.Element("column-names").Elements("column-name").Select(x => x.Value).ToList();
                if (!columnNames.Contains("Date"))
                {
                    continue;
                }
                if (!(columnNames.Contains("Close") || columnNames.Contains("Settle")))
                {
                    continue;
                }

                instruments.Add(inst);
            }

            return instruments;
        }
    }
}
