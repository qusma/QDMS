// -----------------------------------------------------------------------
// <copyright file="BarChartUtils.cs" company="">
// Copyright 2015 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace QDMS.Server.DataSources.BarChart
{
    public static class BarChartUtils
    {

        public static List<OHLCBar> ParseJson(JObject data, HistoricalDataRequest request)
        {
            var bars = new List<OHLCBar>();
            var barTimeSpan = request.Frequency.ToTimeSpan();
            Dictionary<int, InstrumentSession> sessionStartTimesByDay =
                request.Instrument.SessionStartTimesByDay();
            var exchangeTz = TimeZoneInfo.FindSystemTimeZoneById(request.Instrument.Exchange.Timezone);

            JToken jsonBars = data["results"];
            foreach (JToken jsonBar in jsonBars)
            {
                var bar = new OHLCBar
                {
                    Open = decimal.Parse(jsonBar["open"].ToString()),
                    High = decimal.Parse(jsonBar["high"].ToString()),
                    Low = decimal.Parse(jsonBar["low"].ToString()),
                    Close = decimal.Parse(jsonBar["close"].ToString())
                };

                long volume;
                if (long.TryParse(jsonBar.Value<string>("volume"), out volume))
                {
                    bar.Volume = volume;
                }

                if (request.Frequency < BarSize.OneDay)
                {
                    //The timezone in which the data is delivered is NOT the exchange TZ
                    //it seems to change depending on the whim of the api and/or location of user?!?!
                    //anyway, we make sure the time is in the exchange's timezone
                    bar.DTOpen = TimeZoneInfo.ConvertTime(DateTimeOffset.Parse(jsonBar["timestamp"].ToString()), exchangeTz).DateTime;
                    bar.DT = bar.DTOpen.Value + barTimeSpan;

                    //For intraday bars, the time is the bar's OPENING time
                    //But it fails to "fit" properly...if you ask for hourly bars
                    //it'll start at 9AM instead of 9:30AM open. So we use the instrument sessions to correct this.
                    //closing time stays the same

                    int dayOfWeek = (int)bar.DTOpen.Value.DayOfWeek -1;
                    if (dayOfWeek < 0) dayOfWeek = 6; //small fix due to DayOfWeek vs DayOfTheWeek different format
                    if (sessionStartTimesByDay.ContainsKey(dayOfWeek) && 
                        bar.DTOpen.Value.TimeOfDay < sessionStartTimesByDay[dayOfWeek].OpeningTime)
                    {
                        bar.DTOpen = bar.DTOpen.Value.Date.Add(sessionStartTimesByDay[dayOfWeek].OpeningTime);
                    }
                }
                else
                {
                    //daily bars or lower frequencies - here the time is provided as "2015-08-17T00:00:00-04:00",
                    //i.e. just the day, not the actual closing time. We only need the date portion, so we parse the tradingDay field.
                    bar.DT = DateTime.Parse(jsonBar["tradingDay"].ToString());
                }

                string openInterest = jsonBar.Value<string>("openInterest");
                if (!string.IsNullOrEmpty(openInterest))
                {
                    bar.OpenInterest = int.Parse(openInterest);
                }

                bars.Add(bar);
            }

            return bars;
        }
    }
}
