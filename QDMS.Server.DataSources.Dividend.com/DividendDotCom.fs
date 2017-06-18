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
    "total": 223,
	"rows": [
		{
			"symbol": "<a href=\"/dividend-stocks/financial/closed-end-fund-equity/gab-gabelli-equity-trust-inc/\"><span class=\"caps\">GAB</span></a>",
			"_symbol_data": {
				"th": "Stock Symbol"
			},
			"name": "<a href=\"/dividend-stocks/financial/closed-end-fund-equity/gab-gabelli-equity-trust-inc/\">Gabelli Equity Trust Inc.</a>",
			"_name_data": {
				"th": "Company Name"
			},
			"dars_rating": "<a href='/premium/signup.php' class='restricted' style='vertical-align: middle;'></a>",
			"_dars_rating_data": {
				"th": "DARS™ Rating"
			},
			"ex_div_date": "<span style=\"white-space: nowrap;margin-right: 20px;\">2017-06-15</span>",
			"_ex_div_date_data": {
				"th": "Ex-Div Date"
			},
			"pay_date": "<span style=\"white-space: nowrap;\">2017-06-26</span>",
			"_pay_date_data": {
				"th": "Pay Date"
			},
			"payout": "0.15",
			"_payout_data": {
				"th": "Div Payout"
			},
			"qualification": "Unknown",
			"_qualification_data": {
				"th": "Qualified Dividend?"
			},
			"price": "$6.13",
			"_price_data": {
				"th": "Stock Price"
			},
			"dividend_yield": "9.79%",
			"_dividend_yield_data": {
				"th": "Yield"
			}
		}
]
}
 """>

    type DividendDotCom() = 
        let error = Event<EventHandler<ErrorArgs>,ErrorArgs>()
        let logger = NLog.LogManager.GetCurrentClassLogger()
        let httpClient = new HttpClient()
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
       
        member this.parse(json : string) =
            let data = jsonProv.Parse(json)
            data.Rows |> Seq.map this.parseRow
                        
        member this.parseRow(row) =
            new Dividend(
                Amount = row.Payout,
                Symbol = (row.Symbol |> removeTags),
                PaymentDate = (row.PayDate |> parseNullableDate),
                Currency = "USD",
                ExDate = parseDate(row.ExDivDate))

        member this.getDividends(request : DividendRequest) = 
            async {
                let url = sprintf "http://www.dividend.com/data_set/?tm=30&cond={\"by_active\":null,\"by_ex_dividend_date\":[\"%s\",\"%s\"],\"by_stock_type\":{\"Preferred\":\"on\",\"ADR\":\"on\",\"ETF\":\"on\",\"ETN\":\"on\",\"Fund\":\"on\",\"SeniorNotes\":\"on\",\"common_shares\":\"on\"}}&no_null_sort=true&count_by_id=&sort=ex_div_date&order=ASC&limit=10000&offset=0" (request.FromDate.ToString("yyyy-MM-dd")) (request.ToDate.ToString("yyyy-MM-dd"))
                logger.Info("Downloading dividends from " + url)

                let! httpResponse = httpClient.GetAsync(url) |> Async.AwaitTask
                httpResponse.EnsureSuccessStatusCode() |> ignore
                let! json = httpResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
                return json 
                    //|> System.Text.RegularExpressions.Regex.Unescape
                    |> this.parse
                    |> System.Collections.Generic.List<Dividend>
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
                    if not (String.IsNullOrEmpty(request.Symbol)) then
                        failwith "Dividend.com does not support symbol-based requests";

                    try
                        return! this.getDividends(request)
                    with e -> 
                            logger.Error(e, "Error downloading dividend data: " + e.Message)
                            return System.Collections.Generic.List<Dividend>()
                } |> Async.StartAsTask