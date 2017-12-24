namespace QDMS.Server.DataSources

open QDMS
open System
open NLog
open System.Net.Http
open FSharp.Data
open System.Text.RegularExpressions
open System.Globalization
//CBOE has data on dividends, earnings, splits, guidance, and economic events

module CBOEModule = 

    type htmlProv = HtmlProvider<"./CBOE.html", PreferOptionals=true,IncludeLayoutTables=true,Culture="en-US">

    type CBOE() = 
        let error = Event<EventHandler<ErrorArgs>,ErrorArgs>()
        let logger = NLog.LogManager.GetCurrentClassLogger()
        let httpClient = new HttpClient()
        
        let removeTags (data : string) = Regex.Replace(data, @"<[^>]*>", String.Empty)

        let extractTitle (str) = 
            let regexMatch = Regex.Match(str, "<a[^>]* title=\"([^\"]*)")
            match regexMatch.Success with
            | true -> regexMatch.Value
            | false -> 
                logger.Error("Failed to parse company name: " + str)
                ""

        let parseNullableDecimal (str) = 
            match str with
            | Some value -> 
                match Decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture) with
                | (true, num) -> Nullable<decimal>(num)
                | _ -> System.Nullable()
            | None -> System.Nullable()

        let parseEarningsCallTime (str) =
            match str with
            | "BMO" -> EarningsCallTime.BeforeMarketOpen
            | "AMC" -> EarningsCallTime.AfterMarketClose
            | _ -> EarningsCallTime.NotAvailable

        member this.getAnnouncementsForDate (date : DateTime) = 
            async {
                let timestamp = MyUtils.ConvertToTimestamp(date)
                let url = sprintf "https://hosted-calendar.zacks.com/zackscal/retrieve_eventsdata/1/%i/cboe" timestamp
                logger.Info("Downloading earnings from " + url)
                let! httpResponse = httpClient.GetAsync(url) |> Async.AwaitTask
                httpResponse.EnsureSuccessStatusCode() |> ignore

                let! html = httpResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
                return this.parse(html, date.Date)
            }

        member this.parse(html, date) = 
            let asdf = htmlProv.Parse(html)
            asdf.Tables.Events_results.Rows
                |> Seq.map(fun row -> new EarningsAnnouncement(Symbol = row.Symbol,
                                                                CompanyName = row.Company,
                                                                //todo get raw HTML instead and extract correct title from "title" tag
                                                                Date = date,
                                                                EarningsPerShare = parseNullableDecimal row.Reported,
                                                                Forecast = parseNullableDecimal row.Estimate,
                                                                EarningsCallTime = parseEarningsCallTime row.Time))


        member this.getAnnouncements (request : EarningsAnnouncementRequest) =
            async {
                let dayCount = (int) ((request.ToDate.Date - request.FromDate.Date).TotalDays + 0.5)
                let dates = [0..dayCount] 
                                |> Seq.map(fun x -> request.FromDate.Date.AddHours(5.0).AddDays(float x)) 
                                |> Seq.filter(fun x -> x.DayOfWeek <> DayOfWeek.Saturday && x.DayOfWeek <> DayOfWeek.Sunday) //filter weekends

                let earnings = new System.Collections.Generic.List<EarningsAnnouncement>()
                for date in dates do
                    let! earnsForDate = this.getAnnouncementsForDate date
                    earnings.AddRange(earnsForDate)

                return earnings;
            }

        interface IEarningsAnnouncementSource with
            [<CLIEvent>]
            member this.Error = error.Publish
            member this.Connected = true
            member this.Name = "CBOE"
            member this.Disconnect() = ()
            member this.Connect() = ()

            member this.RequestData(request: EarningsAnnouncementRequest) = 
                async {
                    if (request.Symbol <> null && request.Symbol.Count > 0) then
                        failwith "CBOE does not support symbol-based requests";

                    try
                        return! this.getAnnouncements(request)
                    with e -> 
                            logger.Error(e, "Error downloading announcements data: " + e.Message)
                            return System.Collections.Generic.List<EarningsAnnouncement>()
                } |> Async.StartAsTask
