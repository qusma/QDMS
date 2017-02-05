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

    type SymbolDividends = HtmlProvider<"./DataSamples/AAPL.html">
    type DateDividends = HtmlProvider<"./DataSamples/Jan27.html">

    type Nasdaq() = 
        let error = Event<EventHandler<ErrorArgs>,ErrorArgs>()
        let logger = NLog.LogManager.GetCurrentClassLogger()
        let httpClient = new HttpClient()

        let parseNullableDate (str) = 
            match DateTime.TryParseExact(str, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None) with
            | (true, num) -> Nullable<DateTime>(num)
            | _ -> Nullable<DateTime>() //TODO not working properly, also 

        let parseDate str = 
             DateTime.ParseExact(str, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None)

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

        member this.getDividendsForDate(date : DateTime) =
            async {
                let url = sprintf "http://www.nasdaq.com/dividend-stocks/dividend-calendar.aspx?date=%s" (date.ToString("yyyy-MMM-dd"))
                logger.Info("Downloading dividends from " + url)

                let! data = DateDividends.AsyncLoad(url)

                return data.Html.Body().Descendants(fun x -> x.HasId("Table1")).First().Descendants ["tr"]
                    |> Seq.skip 1
                    |> Seq.map(fun tr -> tr.Descendants ["td"] |> Seq.map(fun td -> td.InnerText()) |> Seq.toArray)
                    |> Seq.map(fun x -> new Dividend(Symbol = Regex.Match(x.[0], @"\([^\)]*\)$").Value.Trim('(', ')'),
                                                     ExDate = parseDate x.[1],
                                                     Amount = Decimal.Parse x.[2],
                                                     RecordDate = parseNullableDate x.[4],
                                                     DeclarationDate = parseNullableDate x.[5],
                                                     PaymentDate = parseNullableDate x.[6])) 
            }


        member this.getSpecificSymbol(request: DividendRequest) =
            async {
                let url = sprintf "http://www.nasdaq.com/symbol/%s/dividend-history" request.Symbol
                logger.Info("Downloading dividends from " + url)
                let! httpResponse = httpClient.GetAsync(url) |> Async.AwaitTask
                httpResponse.EnsureSuccessStatusCode() |> ignore
                let! html = httpResponse.Content.ReadAsStreamAsync() |> Async.AwaitTask
                let data = SymbolDividends.Load(html) //todo need to use httpclient for the download
                return data.Tables.Quotes_content_left_dividendhistoryGrid.Rows |> this.parseSymbolRows request
            }

        member this.parseSymbolRows request rows = 
            let divs = rows 
                    |> Seq.filter(fun row -> row.``Ex/Eff Date`` >= request.FromDate.Date && row.``Ex/Eff Date`` <= request.ToDate.Date)
                    |> Seq.map(fun row -> new Dividend(
                                                        Amount = row.``Cash Amount``, 
                                                        ExDate = row.``Ex/Eff Date``,
                                                        DeclarationDate = parseNullableDate row.``Declaration Date``,
                                                        RecordDate = new Nullable<DateTime>(row.``Record Date``),
                                                        PaymentDate = parseNullableDate row.``Payment Date``,
                                                        Type = row.Type,
                                                        Symbol = request.Symbol))

            new System.Collections.Generic.List<Dividend>(divs)

        member this.getDividends(request: DividendRequest) = 
            async {
                let! data = match String.IsNullOrEmpty(request.Symbol) with
                                    | true -> this.getAllSymbols(request)
                                    | false -> this.getSpecificSymbol(request)
                return data
            }

        interface IDividendDataSource with
            [<CLIEvent>]
            member this.Error = error.Publish
            member this.Connected = true
            member this.Name = "Nasdaq"
            member this.Disconnect() = ()
            member this.Connect() = ()

            member this.RequestData(request: DividendRequest) = 
                this.getDividends(request) |> Async.StartAsTask