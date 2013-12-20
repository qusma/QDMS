// -----------------------------------------------------------------------
// <copyright file="QuandlUtilsTest.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using NUnit.Framework;
using QDMS;
using QDMSServer;

namespace QDMSTest
{
    [TestFixture]
    public class QuandlUtilsTest
    {
        [Test]
        public void ParseXMLCorrectlyParsesData()
        {
            var targetBars = new List<OHLCBar>
            {
                new OHLCBar {DT = new DateTime(2013,12,12), Open = 97.55m, High = 98.18m, Low = 97.31m, Close = 97.50m, Volume = 153787},
                new OHLCBar {DT = new DateTime(2013,12,11), Open = 98.65m, High = 98.75m, Low = 97.2m, Close = 97.44m, Volume = 189430},
                new OHLCBar {DT = new DateTime(2013,12,10), Open = 97.25m, High = 98.74m, Low = 97.24m, Close = 98.51m, Volume = 218380},
            };


            string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?> <dataset> <errors> </errors> <id type=\"integer\">2298870</id> <source-code>OFDP</source-code> <code>FUTURE_CL1</code> <name>NYMEX Crude Oil Futures, Continuous Contract #1 (CL1) (Front Month)</name> <urlize-name>NYMEX-Crude-Oil-Futures-Continuous-Contract-1-CL1-Front-Month</urlize-name> <description> &lt;p&gt;Historical Futures Prices: Crude Oil Futures, Continuous Contract #1. Non-adjusted price based on spot-month continuous contract calculations. Raw futures data from New York Mercantile Exchange (NYMEX). &lt;/p&gt; </description> <updated-at>2013-12-13T01:59:28Z</updated-at> <frequency>daily</frequency> <from-date>1983-03-30</from-date> <to-date>2013-12-12</to-date> <column-names type=\"array\"> <column-name>Date</column-name> <column-name>Open</column-name> <column-name>High</column-name> <column-name>Low</column-name> <column-name>Settle</column-name> <column-name>Volume</column-name> <column-name>Open Interest</column-name> </column-names> <private type=\"boolean\">false</private> <type nil=\"true\"/> <display-url>http://www.ofdp.org/continuous_contracts/data?exchange=NYM&amp;symbol=CL&amp;depth=1</display-url> <data type=\"array\"> <datum type=\"array\"> <datum>2013-12-12</datum> <datum type=\"float\">97.55</datum> <datum type=\"float\">98.18</datum> <datum type=\"float\">97.31</datum> <datum type=\"float\">97.5</datum> <datum nil=\"true\"/> <datum type=\"float\">153787.0</datum> </datum> <datum type=\"array\"> <datum>2013-12-11</datum> <datum type=\"float\">98.65</datum> <datum type=\"float\">98.75</datum> <datum type=\"float\">97.2</datum> <datum type=\"float\">97.44</datum> <datum nil=\"true\"/> <datum type=\"float\">189430.0</datum> </datum> <datum type=\"array\"> <datum>2013-12-10</datum> <datum type=\"float\">97.25</datum> <datum type=\"float\">98.74</datum> <datum type=\"float\">97.24</datum> <datum type=\"float\">98.51</datum> <datum nil=\"true\"/> <datum type=\"float\">218380.0</datum> </datum> </data> </dataset> ";
            List<OHLCBar> bars = QuandlUtils.ParseXML(xml);

            Assert.AreEqual(targetBars.Count, bars.Count);

            for (int i = 0; i < bars.Count; i++)
            {
                Assert.AreEqual(targetBars[i].LongDate, bars[i].LongDate);
                Assert.AreEqual(targetBars[i].Open, bars[i].Open);
                Assert.AreEqual(targetBars[i].High, bars[i].High);
                Assert.AreEqual(targetBars[i].Low, bars[i].Low);
                Assert.AreEqual(targetBars[i].Close, bars[i].Close);
                Assert.AreEqual(targetBars[i].Volume, bars[i].Volume);
            }
        }

        [Test]
        public void InstrumentParsingWorksProperly()
        {
            string xml = "<datasets> <total-count type=\"integer\">232701</total-count> <current-page type=\"integer\">2</current-page> <per-page type=\"integer\">20</per-page> <docs type=\"array\"> <doc> <id type=\"integer\">2316351</id> <source-code>DOE</source-code> <code>EIA_TOTALOILSUPPLY_A_OMAN127</code> <name>Total Oil Supply: Oman</name> <urlize-name>Total-Oil-Supply-Oman</urlize-name> <description> Units=Thousand Barrels Per Day. The U.S. Energy Information Administration (EIA) collects, analyzes, and disseminates independent and impartial energy information to promote sound policymaking, efficient markets, and public understanding of energy and its interaction with the economy and the environment. EIA provides a wide range of information and data products covering energy production, stocks, demand, imports, exports, and prices; and prepares analyses and special reports on topics of current interest. </description> <updated-at>2013-11-29T15:49:08Z</updated-at> <frequency>annual</frequency> <from-date>2007-12-31</from-date> <to-date>2011-12-31</to-date> <column-names type=\"array\"> <column-name>Year</column-name> <column-name>Thousand Barrels Per Day</column-name> </column-names> <private type=\"boolean\">false</private> <type nil=\"true\" /> <display-url>http://www.eia.gov/cfapps/ipdbproject/XMLinclude_3.cfm?tid=5&amp;pid=53&amp;pdid=53,55,57,58,59,56,54,62,63,64,65,66,67,68&amp;aid=1&amp;cid=regions&amp;titleStr=Total%20Oil%20Supply%20(Thousand%20Barrels%20Per%20Day)&amp;syid=2007&amp;eyid=2011&amp;form=&amp;defaultid=3&amp;typeOfUnit=STDUNIT&amp;unit=TBPD&amp;products= </display-url> </doc> </docs> </datasets>";
            int count;
            var instrument = QuandlUtils.ParseInstrumentXML(xml, out count)[0];

            Assert.AreEqual(1, count);
            Assert.AreEqual("DOE/EIA_TOTALOILSUPPLY_A_OMAN127", instrument.DatasourceSymbol);
            Assert.AreEqual("EIA_TOTALOILSUPPLY_A_OMAN127", instrument.Symbol);
            Assert.AreEqual("Total Oil Supply: Oman", instrument.Name);
        }
    }
}
