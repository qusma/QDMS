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

    type SymbolDividends = HtmlProvider<"./AAPL.html", IncludeLayoutTables=true, PreferOptionals=true>
    type DateDividends = HtmlProvider<"./Jan27.html", IncludeLayoutTables=true, PreferOptionals=true>

    type Nasdaq() = 
        let error = Event<EventHandler<ErrorArgs>,ErrorArgs>()
        let logger = NLog.LogManager.GetCurrentClassLogger()
        let httpClient = new HttpClient()

        let parseNullableDate (str) = 
            match str with
            | Some value -> 
                match DateTime.TryParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | (true, num) -> Nullable<DateTime>(num)
                | _ -> System.Nullable()
            | None -> System.Nullable()

        let parseDate str = 
             DateTime.ParseExact(str, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None)

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
                let url = sprintf "http://www.nasdaq.com/dividend-stocks/dividend-calendar.aspx?date=%s" (date.ToString("yyyy-MMM-dd"))
                logger.Info("Downloading dividends from " + url)

                let! html = this.getStrFromUrl(url)
                try 
                    let data = DateDividends.Load(html)
                    return data.Tables.Table1.Rows
                        |> Seq.map(this.parseRow) 
                with e -> 
                    logger.Error(e, "Failed to parse dividends on " + date.ToString() + ". No divs available?")
                    return Seq.empty<Dividend>
            }

            
        member this.parseRow row =
            new Dividend(Symbol = Regex.Match(row.``Company (Symbol)``, @"\([^\)]*\)$").Value.Trim('(', ')'),
                                                     ExDate = row.``Ex-Dividend Date``,
                                                     Amount = row.Dividend,
                                                     RecordDate = parseNullableDate row.``Record Date``,
                                                     DeclarationDate = parseNullableDate row.``Announcement Date``,
                                                     PaymentDate = parseNullableDate row.``Payment Date``)


        member this.getSpecificSymbol(request: DividendRequest, symbol: string) =
            async {
                let url = sprintf "http://www.nasdaq.com/symbol/%s/dividend-history" symbol
                logger.Info("Downloading dividends from " + url)
                let! html = this.getStrFromUrl(url)
                let data = SymbolDividends.Load(html)
                return data.Tables.Quotes_content_left_dividendhistoryGrid.Rows |> this.parseSymbolRows request symbol
            }

        member this.parseSymbolRows request symbol rows = 
            rows 
                    |> Seq.filter(fun row -> row.``Ex/Eff Date`` >= request.FromDate.Date && row.``Ex/Eff Date`` <= request.ToDate.Date)
                    |> Seq.map(fun row -> new Dividend(
                                                        Amount = row.``Cash Amount``, 
                                                        ExDate = row.``Ex/Eff Date``,
                                                        DeclarationDate = parseNullableDate row.``Declaration Date``,
                                                        RecordDate = parseNullableDate row.``Record Date``,
                                                        PaymentDate = parseNullableDate row.``Payment Date``,
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
                            return System.Collections.Generic.List<Dividend>()
                } |> Async.StartAsTask