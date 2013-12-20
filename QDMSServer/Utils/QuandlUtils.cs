// -----------------------------------------------------------------------
// <copyright file="QuandlUtils.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using System.Linq;
using QDMS;

namespace QDMSServer
{
    public static class QuandlUtils
    {
        /// <summary>
        /// Takes XML formated data from Quandl and turns it into OHLCBars.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static List<OHLCBar> ParseXML(string data)
        {
            var doc = XDocument.Parse(data);
            var columns = new List<string>();
            var bars = new List<OHLCBar>();

            if (doc.Root == null)
            {
                throw new Exception("XML Parse error: no root.");
            }

            //start by checking what columns are included in the data...
            //in case of insufficient columns we might get out right here
            if (doc.Root.Element("column-names") == null)
            {
                throw new Exception("Quandl: Column names element not found.");
            }


            columns = doc.Root.Element("column-names").Elements("column-name").Select(x => x.Value).ToList();


            bool skipBar, dateSet;

            var dataElement = doc.Root.Element("data");
            if (dataElement == null)
            {
                throw new Exception("No data.");
            }

            foreach (var barElements in dataElement.Elements("datum"))
            {
                skipBar = false;
                dateSet = false;
                decimal parsedValue;

                var bar = new OHLCBar();
                int counter = 0;
                foreach (var price in barElements.Elements("datum"))
                {
                    bool isNull = price.Attribute("nil") != null; //if the attribute "nil" exists, then the value is null

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
                            if (decimal.TryParse(price.Value, out parsedValue))
                                bar.Open = parsedValue;
                            break;

                        case "High":
                            if (decimal.TryParse(price.Value, out parsedValue))
                                bar.High = parsedValue;
                            break;

                        case "Low":
                            if (decimal.TryParse(price.Value, out parsedValue))
                                bar.Low = parsedValue;
                            break;

                        case "Close":
                            if (decimal.TryParse(price.Value, out parsedValue))
                                bar.Close = parsedValue;
                            break;

                        case "Settle": //some futures data series have "settle" field instead of "close"
                            if (!isNull && decimal.TryParse(price.Value, out parsedValue))
                                bar.Close = parsedValue;
                            break;

                            //volume and OI are not represented as ints for some reason
                        case "Volume":
                            if (!isNull && decimal.TryParse(price.Value, out parsedValue))
                                bar.Volume = (int)parsedValue;
                            break;

                        case "Open Interest":
                            if (!isNull && decimal.TryParse(price.Value, out parsedValue))
                                bar.Volume = (int)parsedValue;
                            break;

                        case "Adjusted Close":
                            if (decimal.TryParse(price.Value, out parsedValue))
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

            //they are in descending date order, so reverse the list
            bars.Reverse();

            return bars;
        }
    }
}
