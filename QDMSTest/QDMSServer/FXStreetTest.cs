// -----------------------------------------------------------------------
// <copyright file="FXStreetTest.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using NUnit.Framework;
using QDMS;
using QDMS.Server.DataSources;
using System.Collections.Generic;
using System.Linq;

namespace QDMSTest
{
    [TestFixture]
    public class FXStreetTest
    {
        private string _data = @"DateTime,Name,Country,Volatility,Actual,Previous,Consensus
10/13/2016 00:00:00,Consumer Inflation Expectation,Australia,2,3.700,3.300,
10/13/2016 00:58:00,BoK Interest Rate Decision,South Korea,1,1.250,1.250,1.250
10/13/2016 02:00:00,FDI - Foreign Direct Investment (YTD) (YoY),China,2,4.200,4.500,
10/13/2016 02:30:00,Trade Balance CNY,China,2,278.400,346.000,300.000
10/13/2016 02:40:00,Imports (YoY),China,2,-1.900,1.500,1.000
10/13/2016 02:40:00,Exports (YoY),China,2,-10.000,-2.800,-3.000
10/13/2016 02:40:00,Trade Balance USD,China,2,41.989,52.050,53.000
10/13/2016 04:30:00,Tertiary Industry Index (MoM),Japan,2,0.000,0.300,
10/13/2016 06:00:00,Harmonised Index of Consumer Prices (MoM),Germany,2,0.000,0.000,0.000
10/13/2016 06:00:00,Consumer Price Index (YoY),Germany,2,0.700,0.700,0.700
10/13/2016 06:00:00,Harmonised Index of Consumer Prices (YoY),Germany,2,0.500,0.500,0.500
10/13/2016 06:00:00,Consumer Price Index (MoM),Germany,2,0.100,0.100,0.100
10/13/2016 07:00:00,Headline Inflation (YoY),Slovakia,1,-0.500,-0.800,
10/13/2016 07:00:00,Headline Inflation (MoM),Slovakia,1,0.100,-0.100,
10/13/2016 07:00:00,Core Inflation (YoY),Slovakia,1,0.100,-0.200,
10/13/2016 07:00:00,Core Inflation (MoM),Slovakia,1,0.100,-0.100,
10/13/2016 12:30:00,Continuing Jobless Claims,United States,1,2.046,2.058,
10/13/2016 12:30:00,Initial Jobless Claims,United States,2,246.000,249.000,254.000
10/13/2016 12:30:00,Export Price Index (MoM),United States,2,0.300,-0.800,0.000
10/13/2016 12:30:00,Import Price Index (YoY),United States,2,-1.100,-2.200,
10/13/2016 12:30:00,Export Price Index (YoY),United States,2,-1.500,-2.400,
10/13/2016 12:30:00,Import Price Index (MoM),United States,2,0.100,-0.200,0.200
10/13/2016 12:30:00,New Housing Price Index (YoY),Canada,1,2.700,2.800,
10/13/2016 12:30:00,New Housing Price Index (MoM),Canada,1,0.200,0.400,0.300
10/13/2016 14:30:00,EIA Natural Gas Storage change,United States,1,,80.000,87.000
10/13/2016 15:00:00,EIA Crude Oil Stocks change,United States,1,4.900,-2.976,0.650
10/13/2016 16:15:00,FOMC Member Harker Speech,United States,2,,,
10/13/2016 17:00:00,30-Year Bond Auction,United States,1,2.470,2.475,
10/13/2016 19:00:00,Consumer Price Index (MoM),Argentina,1,1.100,0.200,
10/13/2016 21:00:00,Export Price Growth (YoY),South Korea,1,-8.300,-9.700,
10/13/2016 21:00:00,Import Price Growth (YoY),South Korea,1,-7.800,-8.500,
10/13/2016 23:00:00,Interest rate decision,Peru,1,,4.250,
10/13/2016 23:50:00,Domestic Corporate Goods Price Index (MoM),Japan,1,0.000,-0.300,-0.100
10/13/2016 23:50:00,Domestic Corporate Goods Price Index (YoY),Japan,1,-3.200,-3.600,-3.200
10/13/2016 23:50:00,Money Supply M2+CD (YoY),Japan,1,3.600,3.300,
10/13/2016 23:50:00,Foreign investment in Japan stocks,Japan,2,430.300,251.700,
10/13/2016 23:50:00,Foreign bond investment,Japan,2,-737.700,-636.800,
10/14/2016 00:00:00,Gross Domestic Product (QoQ),Singapore,1,-4.100,0.300,
10/14/2016 00:00:00,Gross Domestic Product (YoY),Singapore,1,0.600,2.100,
10/14/2016 00:30:00,Financial Stability Review,Australia,1,,,
10/14/2016 01:30:00,Producer Price Index (YoY),China,2,0.100,-0.800,-0.300
10/14/2016 01:30:00,Consumer Price Index (MoM),China,2,0.700,0.100,0.300
10/14/2016 01:30:00,Consumer Price Index (YoY),China,3,1.900,1.300,1.600
10/14/2016 04:30:00,Retail Sales (YoY),""Netherlands, The"",1,1.500,0.900,
10/14/2016 05:00:00,Retail Sales(YoY),Singapore,1,-1.000,2.800,
10/14/2016 05:00:00,Retail Sales(MoM),Singapore,1,-1.100,1.400,
10/14/2016 06:00:00,Gross Domestic Product(YoY),Finland,1,-0.100,1.500,
10/14/2016 06:00:00,Current Account, Finland,1,-0.200,-0.400,
10/14/2016 06:00:00, Consumer Price Index(YoY),Finland,1,0.400,0.400,
10/14/2016 06:30:00,WPI Inflation, India,1,3.570,3.740,
10/14/2016 07:00:00, Consumer Price Index(YoY),Spain,1,0.200,-0.100,0.300
10/14/2016 07:00:00,HICP(YoY),Spain,1,0.000,0.100,0.100
10/14/2016 07:00:00,HICP(MoM),Spain,1,0.700,0.000,0.800
10/14/2016 07:00:00,Consumer Price Index(MoM),Spain,1,0.000,0.100,0.100
10/14/2016 07:15:00,Producer and Import Prices(MoM),Switzerland,1,0.300,-0.300,0.100
10/14/2016 07:15:00,Producer and Import Prices(YoY),Switzerland,1,-0.100,-0.400,-0.200
10/14/2016 08:30:00,BOE Credit Conditions Survey, United Kingdom,1,,,
10/14/2016 09:00:00,Consumer Price Index(EU Norm) (YoY),Italy,1,0.100,0.100,0.100
10/14/2016 09:00:00,Consumer Price Index(MoM),Italy,1,-0.200,-0.200,-0.200
10/14/2016 09:00:00,Consumer Price Index(EU Norm) (MoM),Italy,1,1.900,1.900,1.900
10/14/2016 09:00:00,Consumer Price Index(YoY),Italy,1,0.100,0.100,0.100
10/14/2016 09:00:00,Trade Balance n.s.a.,European Monetary Union,2,18.400,25.300,15.300
10/14/2016 09:00:00,Trade Balance s.a.,European Monetary Union,2,23.300,20.000,20.500
10/14/2016 11:30:00,""FX Reserves, USD"",India,1,367.650,371.990,
10/14/2016 12:00:00,Current Account, Poland,1,-1047.000,-802.000,
10/14/2016 12:00:00, Trade Deficit Government, India,1,8.340,7.670,
10/14/2016 12:30:00, Retail Sales ex Autos(MoM),United States,2,0.500,-0.100,0.400
10/14/2016 12:30:00, Retail Sales control group,United States,2,0.100,-0.100,0.400
10/14/2016 12:30:00, Retail Sales(MoM),United States,3,0.600,-0.300,0.600
10/14/2016 12:30:00, Producer Price Index(MoM),United States,1,0.300,0.000,0.200
10/14/2016 12:30:00, Producer Price Index ex Food & Energy(YoY),United States,1,1.200,1.000,1.200
10/14/2016 12:30:00, Producer Price Index(YoY),United States,1,0.700,0.000,0.600
10/14/2016 12:30:00, Producer Price Index ex Food & Energy(MoM),United States,1,0.200,0.100,0.200
10/14/2016 12:30:00, Federal Reserve Bank of Boston President Rosengren Speech,United States,1,,,
10/14/2016 12:30:00, M3 Money Supply(YoY),Poland,1,9.300,10.000,
10/14/2016 14:00:00,Reuters/Michigan Consumer Sentiment Index, United States,3,87.900,91.200,91.900
10/14/2016 14:00:00,Business Inventories, United States,1,0.200,0.000,0.200
10/14/2016 14:00:00,BOE's Governor Carney speech,United Kingdom,3,,,
10/14/2016 17:00:00,Baker Hughes US Oil Rig Count, United States,1,432.000,428.000,
10/14/2016 17:30:00,Fed's Yellen Speech,United States,3,,,
10/14/2016 19:30:00,CFTC Gold NC net positions,United States,1,,245.500,
10/14/2016 19:30:00, CFTC USD NC net positions, United States,1,,45.100,
10/14/2016 19:30:00,CFTC Oil NC net positions,United States,1,,363.000,
10/14/2016 19:30:00, CFTC GBP NC net positions, United Kingdom,1,-95.000,-98.000,
10/14/2016 19:30:00,CFTC JPY NC net positions,Japan,1,46.000,69.000,
10/14/2016 19:30:00,CFTC EUR NC net positions,European Monetary Union,1,-93.000,-82.000,
10/14/2016 19:30:00,CFTC AUD NC net positions,Australia,1,26.000,24.000,
10/14/2016 20:00:00,Monthly Budget Statement,United States,2,33.000,-107.000,25.000";

        [Test]
        public void ParsesDataCorrectly()
        {
            var fxStreet = new fx.FXStreet();
            var errors = new List<string>();
            ((IEconomicReleaseSource)fxStreet).Error += (s, e) => errors.Add(e.ErrorMessage);

            var items = fxStreet.parseData(_data);

            foreach (string error in errors)
            {
                TestContext.WriteLine(error);
            }

            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(88, items.Count);
            Assert.IsFalse(items.Any(x => string.IsNullOrEmpty(x.Country)));
            Assert.IsFalse(items.Any(x => string.IsNullOrEmpty(x.Currency)));
            var allNulls = items.FirstOrDefault(x => x.Name == "BOE's Governor Carney speech");
            Assert.AreEqual(null, allNulls.Previous);
            Assert.AreEqual(null, allNulls.Actual);
            Assert.AreEqual(null, allNulls.Expected);

            Assert.AreEqual(1.25, items[1].Previous);
            Assert.AreEqual(1.25, items[1].Actual);
            Assert.AreEqual(1.25, items[1].Expected.Value);
        }
    }
}