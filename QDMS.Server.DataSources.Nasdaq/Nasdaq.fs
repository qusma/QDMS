namespace QDMS.Server.DataSources.Nasdaq
module NasdaqDs =

    open QDMS
    open System
    open System.Linq
    open FSharp.Data
    open NLog
    open System.Text.RegularExpressions
    open System.Globalization
    open System.Net.Http
    open System.Threading.Tasks

    type SymbolDividends = JsonProvider<"./AAPL.json">
    type DateDividends = JsonProvider<"./date.json">

    type Nasdaq() = 
        let error = Event<EventHandler<ErrorArgs>,ErrorArgs>()
        let logger = NLog.LogManager.GetCurrentClassLogger()
        let httpClient = new HttpClient()
        do
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html, application/xhtml+xml, application/xml; q=0.9, */*; q=0.8") |> ignore
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "peerdist") |> ignore
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US") |> ignore

        let parseNullableDate (str) = 
            match str with
            | Some value -> 
                match DateTime.TryParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | (true, num) -> Nullable<DateTime>(num)
                | _ -> System.Nullable()
            | None -> System.Nullable()

        let parseDate str = 
             DateTime.ParseExact(str, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None)

        let strOrDateTimeToNullable(strOrDate : DateDividends.StringOrDateTime) = 
            if strOrDate.DateTime.IsSome then System.Nullable strOrDate.DateTime.Value else System.Nullable<DateTime>()

        member this.nullOrEmpty<'T>(list: System.Collections.Generic.List<'T>) = 
            match list with
            | null -> true
            | l -> l.Count = 0

        member this.getAllSymbols(request: DividendRequest) = 
            async {
                let dayCount = (int) ((request.ToDate.Date - request.FromDate.Date).TotalDays + 0.5)
                let dates = [0..dayCount] 
                                |> Seq.map(fun x -> request.FromDate.AddDays(float x)) 
                                |> Seq.filter(fun x -> x.DayOfWeek <> DayOfWeek.Saturday && x.DayOfWeek <> DayOfWeek.Sunday) //filter weekends
                
                let dividends = new System.Collections.Generic.List<Dividend>()
                for date in dates do
                    let! divs = this.getDividendsForDate date
                    dividends.AddRange(divs)

                return dividends;
            }

        member this.getStrFromUrl(url : string) =
            async {
                let! httpResponse = httpClient.GetAsync(url) |> Async.AwaitTask
                httpResponse.EnsureSuccessStatusCode() |> ignore
                return! httpResponse.Content.ReadAsStreamAsync() |> Async.AwaitTask
            }

        member this.getDividendsForDate(date : DateTime) =
            async {
                let url = sprintf "https://api.nasdaq.com/api/calendar/dividends?date=%s" (date.ToString("yyyy-MM-dd"))
                logger.Info("Downloading dividends from " + url)

                let! json = this.getStrFromUrl(url)
                try 
                    let data = DateDividends.Load(json)
                    return data.Data.Calendar.Rows
                        |> Seq.filter(fun row -> row.DividendExDate.DateTime.IsSome)
                        |> Seq.map(this.parseRow) 
                with e -> 
                    logger.Error(e, "Failed to parse dividends on " + date.ToString() + ". No divs available?")
                    return Seq.empty<Dividend>
            }

            
        member this.parseRow row =
            new Dividend(Symbol = row.Symbol,
                                                     ExDate = row.DividendExDate.DateTime.Value,
                                                     Amount = row.DividendRate,
                                                     RecordDate = strOrDateTimeToNullable row.RecordDate,
                                                     DeclarationDate = strOrDateTimeToNullable row.AnnouncementDate,
                                                     PaymentDate = strOrDateTimeToNullable row.PaymentDate)


        member this.getSpecificSymbol(request: DividendRequest, symbol: string) =
            async {
                let url = sprintf "https://api.nasdaq.com/api/quote/%s/dividends?assetclass=stocks" symbol
                logger.Info("Downloading dividends from " + url)
                let! json = this.getStrFromUrl(url)
                let data = SymbolDividends.Load(json)
                return data.Data.Dividends.Rows |> this.parseSymbolRows request symbol
            }

        member this.parseSymbolRows request symbol rows = 
            rows 
                    |> Seq.filter(fun row -> row.ExOrEffDate >= request.FromDate.Date && row.ExOrEffDate <= request.ToDate.Date)
                    |> Seq.map(fun row -> new Dividend(
                                                        Amount = row.Amount, 
                                                        ExDate = row.ExOrEffDate,
                                                        DeclarationDate = parseNullableDate row.DeclarationDate.String,
                                                        RecordDate = parseNullableDate row.RecordDate.String,
                                                        PaymentDate = parseNullableDate row.PaymentDate.String,
                                                        Type = row.Type,
                                                        Symbol = symbol))

            |> System.Collections.Generic.List<Dividend>

        member this.getSpecificSymbols(request: DividendRequest) =
            async {
                let dividends = new System.Collections.Generic.List<Dividend>()
                for symbol in request.Symbol do
                    let! divs = this.getSpecificSymbol(request, symbol)
                    dividends.AddRange(divs)

                return dividends;
            }

        member this.getDividends(request: DividendRequest) = 
            async {
                return! match this.nullOrEmpty(request.Symbol) with
                                    | true -> this.getAllSymbols(request)
                                    | false -> this.getSpecificSymbols(request)
            }

        interface IDividendDataSource with
            [<CLIEvent>]
            member this.Error = error.Publish
            member this.Connected = true
            member this.Name = "Nasdaq"
            member this.Disconnect() = ()
            member this.Connect() = ()

            member this.RequestData(request: DividendRequest) = 
                async {
                    try
                        return! this.getDividends(request)
                    with e -> 
                            logger.Error(e, "Error downloading dividend data: " + e.Message)
                            error.Trigger(this, new ErrorArgs(0, e.Message))
                            return System.Collections.Generic.List<Dividend>()
                } |> Async.StartAsTask