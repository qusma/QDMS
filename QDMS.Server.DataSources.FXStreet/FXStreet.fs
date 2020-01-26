namespace QDMS.Server.DataSources
module fx = 

    open QDMS
    open System
    open System.Linq
    open FSharp.Data
    open NLog
    open EntityData.Utils

    type FxStreetCalendar = JsonProvider<"./calendar.json">

    type FXStreet(countryCodeHelper:ICountryCodeHelper)  = 
        let countryCodeHelper = countryCodeHelper
        let error = Event<EventHandler<ErrorArgs>,ErrorArgs>()
        let logger = NLog.LogManager.GetCurrentClassLogger()

        let buildUrl(fromDate, toDate) = 
            String.Format("https://calendar-api.fxstreet.com/en/api/v1/eventDates/{0:yyyy-MM-dd}/{1:yyyy-MM-dd}", fromDate, toDate)

        let parseNullable (str) = 
            match Double.TryParse(str) with
            | (true, num) -> Nullable<float>(num)
            | _ -> Nullable<float>()

        let volatilityToImportance (vol) =
            match vol with
            | "HIGH" -> Importance.High
            | "MEDIUM" -> Importance.Mid
            | "LOW" -> Importance.Low
            | _ -> Importance.None

        let optionToNullable (decimal : Option<decimal>) =
            if decimal.IsSome then Nullable<float>(Convert.ToDouble(decimal.Value)) else Nullable<float>()

        member private this.parseRow (row : FxStreetCalendar.Root) = 
            try
                Some(new EconomicRelease(
                                row.Name, 
                                (if row.CountryCode.String.IsSome then row.CountryCode.String.Value else ""), 
                                row.CurrencyCode,
                                row.DateUtc.UtcDateTime, 
                                volatilityToImportance(row.Volatility), 
                                optionToNullable row.Consensus,
                                optionToNullable row.Previous,
                                optionToNullable row.Actual))
            with
                | ex -> 
                    error.Trigger(this, new ErrorArgs(-1, "FXStreet could not parse row: " + row.JsonValue.ToString()  + " " + ex.Message))
                    None

        member this.parseData(json: string) = 
            let parsed = FxStreetCalendar.Parse(json)
            let releases = parsed |> Seq.map(this.parseRow) 
                                    |> Seq.filter(fun x -> x.IsSome)
                                    |> Seq.map(fun x -> x.Value)
            let econReleases = new System.Collections.Generic.List<EconomicRelease>()
            econReleases.AddRange(releases)
            econReleases

        member this.getData(fromDate, toDate) =             
            async {
                try
                    let! json = Http.AsyncRequestString(
                                    buildUrl(fromDate, toDate),
                                    headers = ["Referer", "https://www.fxstreet.com/economic-calendar"]) //it will give 401 if we don't fake the referer
                                    //TODO check if there are limits and we need to split up the request
                    return this.parseData(json)
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