namespace QDMS.Server.DataSources

module DivDotCom =

    open QDMS
    open System
    open System.Net.Http
    open NLog;
    open FSharp.Data;
    open System.Text.RegularExpressions
    open System.Globalization
    open System.Net.Http.Headers
    //sometimes not entirely correct dividend values...
    type jsonProv = FSharp.Data.JsonProvider<"./response.json">

    //can't scrape it any more, too much info hidden...might change in the future
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
            httpClient.DefaultRequestHeaders.Add("Referer", "https://www.dividend.com/ex-dividend-dates/");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://www.dividend.com")
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:72.0) Gecko/20100101 Firefox/72.0")

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
            (data.Meta.TotalPages, data.Data |> Seq.map this.parseRow)
                        
        member this.parseRow(row) =
            new Dividend(
                Amount = row.ClosestPaymentAdjustedAmount,
                Symbol = row.Symbol.Text,
                PaymentDate = new Nullable<DateTime>(row.ClosestPaymentPayableDate),
                Currency = "USD",
                ExDate = row.ClosestPaymentExDate)
        
        member this.dividendSubRequest(request : DividendRequest, page : int) = 
            async {
                
                let url = "https://www.dividend.com/api/data_set/"
                //logger.Info("Downloading dividends from " + url + " offset " + offset.ToString()) 

                let req = new HttpRequestMessage()
                req.Method <- System.Net.Http.HttpMethod.Post
                req.RequestUri <- new Uri(url)
                req.Headers.Accept.Clear()
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"))
                let content = new StringContent(System.String.Format("{{\"f_22_from\":\"{1}\",\"f_22_to\":\"{2}\",\"f_6\":[],\"only\":[\"meta\",\"data\"],\"page\":{0},\"r\":\"Webpage#1280\",\"tm\":\"3-ex-div-dates\"}}", page, request.FromDate.ToString("yyyy-MM-dd"), request.ToDate.ToString("yyyy-MM-dd")) , System.Text.Encoding.UTF8, "application/json")
                let! httpResponse = httpClient.PostAsync(url, content) |> Async.AwaitTask
                httpResponse.EnsureSuccessStatusCode() |> ignore
                let! json = httpResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
                return json |> this.parse
            }

        member this.getDividends(request : DividendRequest) = 
            async {
                ////there are 20 records per "page"
                let divs = new System.Collections.Generic.List<Dividend>()
                let! (totalPages, rows) = this.dividendSubRequest(request, 1)

                divs.AddRange(rows)
                let mutable counter = 2
                while counter <= totalPages do
                    let! (_, rows) = this.dividendSubRequest(request, counter)
                    counter <- counter + 1
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