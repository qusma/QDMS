namespace QDMS.Server.DataSources
module fx = 

    open QDMS
    open System
    open System.Linq
    open FSharp.Data
    open NLog

    type FXStreet(countryCodeHelper:CountryCodeHelper)  = 
        let countryCodeHelper = countryCodeHelper
        let error = Event<EventHandler<ErrorArgs>,ErrorArgs>()
        let logger = NLog.LogManager.GetCurrentClassLogger()

        let buildUrl(fromDate, toDate) = 
            String.Format("https://calendar.fxstreet.com/eventdate/?f=csv&v=2&timezone=UTC&rows=&view=range&start={0:yyyyMMdd}&end={1:yyyyMMdd}", fromDate, toDate)

        let parseNullable (str) = 
            match Double.TryParse(str) with
            | (true, num) -> Nullable<float>(num)
            | _ -> Nullable<float>()

        member private this.parseRow(row: CsvRow) = 
            try
                let countryCode = countryCodeHelper.GetCountryCode(row.["Country"])
                let currencyCode = countryCodeHelper.GetCurrencyCode(countryCode)
                Some(new EconomicRelease(
                                row.["Name"], 
                                countryCode, 
                                currencyCode,
                                DateTime.ParseExact(row.["DateTime"], "MM/dd/yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal), 
                                System.Enum.Parse(typeof<Importance>, row.["Volatility"]) :?> Importance, 
                                parseNullable(row.["Consensus"]),
                                parseNullable(row.["Previous"]),
                                parseNullable(row.["Actual"])))
            with
                | ex -> 
                    error.Trigger(this, new ErrorArgs(-1, "FXStreet could not parse row: " + String.Join(", ", row.Columns) + ". " + ex.Message))
                    None

        member this.parseData(content: string) = 
            new System.Collections.Generic.List<EconomicRelease>(CsvFile.Parse(content).Rows |> Seq.choose this.parseRow)

        member this.getData(fromDate, toDate) =             
            async {
                try
                    let! html = Http.AsyncRequestString(
                                    buildUrl(fromDate, toDate),
                                    headers = ["Referer", "https://calendar.fxstreet.com"]) //it will give 401 if we don't fake the referer
                                    //TODO check if there are limits and we need to split up the request
                    return this.parseData(html)
                with
                    | ex -> 
                        error.Trigger(this, new ErrorArgs(-1, "FXStreet could not download data: " + ex.Message))
                        return new System.Collections.Generic.List<EconomicRelease>()
            }

        interface IEconomicReleaseSource with
            [<CLIEvent>]
            member this.Error = error.Publish
            member this.Connected = true
            member this.Name = "FXStreet"
            member this.Disconnect() = ()
            member this.Connect() = ()

            member this.RequestData(fromDate: System.DateTime, toDate: System.DateTime) = 
                this.getData(fromDate, toDate) |> Async.StartAsTask