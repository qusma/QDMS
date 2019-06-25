namespace QDMS.Server.DataSources

module DivDotCom =

    open QDMS
    open System
    open System.Net.Http
    open NLog;
    open FSharp.Data;
    open System.Text.RegularExpressions
    open System.Globalization
    //sometimes not entirely correct dividend values...
    type jsonProv = FSharp.Data.JsonProvider<""" 	
    {  
   "total":3,
   "rows":[  
      {  
         "symbol":"\u003ca href=\"/dividend-stocks/technology/semiconductor-broad-line/cy-cypress-semiconductor/\"\u003eCY\u003c/a\u003e",
         "_symbol_data":{  
            "th":"Stock Symbol"
         },
         "Yields.Stock":"\u003ca href=\"/dividend-stocks/technology/semiconductor-broad-line/cy-cypress-semiconductor/\"\u003eCypress Semiconductor\u003c/a\u003e",
         "_Yields.Stock_data":{  
            "th":"Company Name"
         },
         "dars_rating":"\u003ca href='/free-trial/' class='restricted' style='vertical-align: middle;'\u003e\u003c/a\u003e",
         "_dars_rating_data":{  
            "th":"DARS™ Rating"
         },
         "exdivdate":"\u003cspan style=\"white-space: nowrap;margin-right: 20px;\"\u003e2019-06-26\u003c/span\u003e",
         "_exdivdate_data":{  
            "th":"Ex-Div Date"
         },
         "paydate":"\u003cspan style=\"white-space: nowrap;\"\u003e2019-07-18\u003c/span\u003e",
         "_paydate_data":{  
            "th":"Pay Date"
         },
         "amount":"0.11",
         "_amount_data":{  
            "th":"Div Payout"
         },
         "cache_stocks.qualification":"Unknown",
         "_cache_stocks.qualification_data":{  
            "th":"Qualified Dividend?"
         },
         "cache_stocks.price":"$22.22",
         "_cache_stocks.price_data":{  
            "th":"Stock Price"
         },
         "cache_stocks.dividend_yield":"1.98%",
         "_cache_stocks.dividend_yield_data":{  
            "th":"Yield"
         }
      },
      {  
         "symbol":"\u003ca href=\"/dividend-stocks/financial/foreign-regional-banks/cib-bancolombia-sa/\"\u003e\u003cspan class=\"caps\"\u003eCIB\u003c/span\u003e\u003c/a\u003e",
         "_symbol_data":{  
            "th":"Stock Symbol"
         },
         "Yields.Stock":"\u003ca href=\"/dividend-stocks/financial/foreign-regional-banks/cib-bancolombia-sa/\"\u003eBanColombia S.A.\u003c/a\u003e",
         "_Yields.Stock_data":{  
            "th":"Company Name"
         },
         "dars_rating":"\u003ca href='/free-trial/' class='restricted' style='vertical-align: middle;'\u003e\u003c/a\u003e",
         "_dars_rating_data":{  
            "th":"DARS™ Rating"
         },
         "exdivdate":"\u003cspan style=\"white-space: nowrap;margin-right: 20px;\"\u003e2019-06-26\u003c/span\u003e",
         "_exdivdate_data":{  
            "th":"Ex-Div Date"
         },
         "paydate":"\u003cspan style=\"white-space: nowrap;\"\u003e2019-07-12\u003c/span\u003e",
         "_paydate_data":{  
            "th":"Pay Date"
         },
         "amount":"0.33",
         "_amount_data":{  
            "th":"Div Payout"
         },
         "cache_stocks.qualification":"Unknown",
         "_cache_stocks.qualification_data":{  
            "th":"Qualified Dividend?"
         },
         "cache_stocks.price":"$51.57",
         "_cache_stocks.price_data":{  
            "th":"Stock Price"
         },
         "cache_stocks.dividend_yield":"2.59%",
         "_cache_stocks.dividend_yield_data":{  
            "th":"Yield"
         }
      },
      {  
         "symbol":"\u003ca href=\"/dividend-stocks/financial/regional-mid-atlantic-banks/lion-fidelity-southern-corp/\"\u003e\u003cspan class=\"caps\"\u003eLION\u003c/span\u003e\u003c/a\u003e",
         "_symbol_data":{  
            "th":"Stock Symbol"
         },
         "Yields.Stock":"\u003ca href=\"/dividend-stocks/financial/regional-mid-atlantic-banks/lion-fidelity-southern-corp/\"\u003eFidelity Southern Corp\u003c/a\u003e",
         "_Yields.Stock_data":{  
            "th":"Company Name"
         },
         "dars_rating":"\u003ca href='/free-trial/' class='restricted' style='vertical-align: middle;'\u003e\u003c/a\u003e",
         "_dars_rating_data":{  
            "th":"DARS™ Rating"
         },
         "exdivdate":"\u003cspan style=\"white-space: nowrap;margin-right: 20px;\"\u003e2019-06-26\u003c/span\u003e",
         "_exdivdate_data":{  
            "th":"Ex-Div Date"
         },
         "paydate":"\u003cspan style=\"white-space: nowrap;\"\u003e2019-07-01\u003c/span\u003e",
         "_paydate_data":{  
            "th":"Pay Date"
         },
         "amount":"0.12",
         "_amount_data":{  
            "th":"Div Payout"
         },
         "cache_stocks.qualification":"Qualified",
         "_cache_stocks.qualification_data":{  
            "th":"Qualified Dividend?"
         },
         "cache_stocks.price":"$30.20",
         "_cache_stocks.price_data":{  
            "th":"Stock Price"
         },
         "cache_stocks.dividend_yield":"1.59%",
         "_cache_stocks.dividend_yield_data":{  
            "th":"Yield"
         }
      }
   ]
}
 """>

    type DividendDotCom() = 
        let error = Event<EventHandler<ErrorArgs>,ErrorArgs>()
        let logger = NLog.LogManager.GetCurrentClassLogger()
        let cookieContainer = new System.Net.CookieContainer()
        let rand = new System.Random()
        let handler = new HttpClientHandler()
        do
            handler.CookieContainer <- cookieContainer
        
        let httpClient = new HttpClient(handler)
        do
            httpClient.DefaultRequestHeaders.Add("Referer", "http://www.dividend.com/ex-dividend-dates.php");

        let removeTags (data : string) = Regex.Replace(data, @"<[^>]*>", String.Empty)

        let parseNullableDate (str) = 
            let dateStr = str |> removeTags
            match DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None) with
            | (true, num) -> Nullable<DateTime>(num)
            | _ -> System.Nullable()

        let parseDate str = 
            let dateStr = str |> removeTags
            DateTime.ParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)
       
        let urlDecode str =
            System.Net.WebUtility.UrlDecode str

        member this.clearCookies =
            let cookies = cookieContainer.GetCookies(new Uri("http://www.dividend.com/"))
            for cookie in cookies do
                cookie.Expired <- true

        member this.parse(json : string) =
            let data = jsonProv.Parse(json)
            (data.Total, data.Rows |> Seq.map this.parseRow)
                        
        member this.parseRow(row) =
            new Dividend(
                Amount = row.Amount,
                Symbol = (row.Symbol |> urlDecode |> removeTags),
                PaymentDate = (row.Paydate |> urlDecode |> removeTags |> parseNullableDate),
                Currency = "USD",
                ExDate = (row.Exdivdate |> urlDecode |> removeTags |> parseDate))
        
        member this.dividendSubRequest(request : DividendRequest, offset : int) = 
            async {
                let url = sprintf "http://www.dividend.com/data_set/?tm=362&cond={\"by_active\":null,\"by_ex_div_date\":[\"%s\",\"%s\"],\"by_stock_type\":{\"Preferred\":\"on\",\"ADR\":\"on\",\"ETF\":\"on\",\"ETN\":\"on\",\"Fund\":\"on\",\"REIT\":\"on\",\"SeniorNotes\":\"on\",\"common_stock\":\"on\"}}&no_null_sort=true&count_by_id=" (request.FromDate.ToString("yyyy-MM-dd")) (request.ToDate.ToString("yyyy-MM-dd"))
                logger.Info("Downloading dividends from " + url + " offset " + offset.ToString()) //TODO the problem is that the broker doesn't surface the error...

                let req = new HttpRequestMessage()
                req.Method <- System.Net.Http.HttpMethod.Get
                req.RequestUri <- new Uri(url)
                req.Content <- new StringContent(sprintf "{\"limit\":11,\"offset\":%i,\"order\":\"ASC\",\"sort\":\"exdivdate\"}" offset, System.Text.Encoding.UTF8, "application/json")
                let! httpResponse = httpClient.GetAsync(url) |> Async.AwaitTask
                httpResponse.EnsureSuccessStatusCode() |> ignore
                let! json = httpResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
                return json |> this.parse
            }

        member this.getDividends(request : DividendRequest) = 
            async {
                //do a first request which gets the total, then loop grabbing 10 items each time
                let divs = new System.Collections.Generic.List<Dividend>()
                let! (total, rows) = this.dividendSubRequest(request, 0)
                logger.Info(sprintf "Getting %i dividends" total)
                divs.AddRange(rows)
                let mutable counter = 10
                while counter < total do
                    let! (_, rows) = this.dividendSubRequest(request, counter)
                    counter <- counter + 10
                    divs.AddRange(rows)
                    this.clearCookies
                    do! Async.Sleep(rand.Next(1050, 2000))

                return divs
            }
            //todo if specified symbol, throw
        interface IDividendDataSource with
            [<CLIEvent>]
            member this.Error = error.Publish
            member this.Connected = true
            member this.Name = "DividendDotCom"
            member this.Disconnect() = ()
            member this.Connect() = ()

            member this.RequestData(request: DividendRequest) = 
                async {
                    if (request.Symbol <> null && request.Symbol.Count > 0) then
                        failwith "Dividend.com does not support symbol-based requests";

                    try
                        return! this.getDividends(request)
                    with e -> 
                            logger.Error(e, "Error downloading dividend data: " + e.Message)
                            error.Trigger(this, new ErrorArgs(0, e.Message))
                            return System.Collections.Generic.List<Dividend>()
                } |> Async.StartAsTask