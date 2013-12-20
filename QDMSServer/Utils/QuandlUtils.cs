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
