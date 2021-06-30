module Derrick.Services.GameApiClients.OpenDotaClient

open System
open System.Net
open Newtonsoft.Json
open RestSharp
open Chessie.ErrorHandling
open Derrick.Services.GameApiClients.OpenDotaResponses
open Serilog

let [<Literal>] ApiUrl = "https://api.opendota.com/api"
let ProjectionInfo = [
    "start_time"
    "kills"
    "deaths"
    "assists"
    "xp_per_min"
    "gold_per_min"
    "hero_damage"
    "tower_damage"
    "hero_healing"
    "last_hits"
    "purchase_ward_observer"
    "purchase_ward_sentry"
    
    // Potentially have a situational award if comeback is huge
    // Will need multiple people on the award
    "comeback"
]

let mutable monthRateLimit = 50000
let mutable minuteRateLimit = 60
let mutable requestDates = List.empty

let getApiDelay =
    requestDates <-
        requestDates
        |> List.filter (fun d -> d >= (DateTime.Now.AddMinutes(-1.0)))

    if requestDates.IsEmpty || minuteRateLimit > 0 then
        TimeSpan.Zero
    else
        let oldestRequest =
            requestDates
                |> List.min
        
        oldestRequest.AddMinutes(1.0) - DateTime.Now

let updateRateLimit<'T> (response:IRestResponse) =
    let header =
        response.Headers
        |> Seq.tryFind (fun h -> h.Name = "X-Rate-Limit-Remaining-Minute")
    match header with
    | None -> ()
    | Some h ->  minuteRateLimit <- (Convert.ToInt32(h.Value))
    
    let header =
        response.Headers
        |> Seq.tryFind (fun h -> h.Name = "X-Rate-Limit-Remaining-Month")
    match header with
    | None -> ()
    | Some h ->  monthRateLimit <- (Convert.ToInt32(h.Value))
    
    requestDates <-
        (List.append [DateTime.Now] requestDates)
        
    ok response

let validateStatusOK (response:IRestResponse) =
    if response.StatusCode = HttpStatusCode.OK then
        ok response
    else
        fail $"API request failed. URL: '%s{response.ResponseUri.ToString()}'. \n\
                Status Code: %s{response.StatusCode.ToString()}. \n\
                Content: '%s{response.Content}' \n\
                ErrorMessage: '%s{response.ErrorMessage}'"
        

let serializeData<'T> (response:IRestResponse) =
    try
    ok (JsonConvert.DeserializeObject<'T>(response.Content))
    with
    | _ -> fail "Failure deserializing response body"
        
let processResponse<'T> =
    updateRateLimit
    >> bind validateStatusOK
    >> bind serializeData<'T>
    
let execute<'T> request =
    if monthRateLimit < 50 then
        raise (Exception "Monthly OpenDota rate limit exceeded!")
        
    Async.Sleep getApiDelay |> Async.RunSynchronously
    
    let client = RestClient(ApiUrl)

    let response = client.Execute(request)
        
    match processResponse<'T> response with
    | Pass data -> Some data
    | Fail msg ->
        let errString = String.Join (", ", msg)
        Log.Warning("Failed executing web request. RequestUrl: {Url}. Error: {Error}", response.ResponseUri, $"%s{errString}")
        None
    | Warn _ -> None //Not used as of now

let getPlayerMatches (since:DateTime) accountId  =
    async {
        let path = $"/players/%s{accountId}/matches"
        let request = RestRequest(path)
        
        let daysPrevious = (DateTime.Now - since).Days + 1
        
        request.AddQueryParameter ("date", (string daysPrevious)) |> ignore
        for item in ProjectionInfo do
            request.AddQueryParameter ("project", item) |> ignore
                
        return
            match execute<PlayerMatch list> request with
            | Some matches ->
                matches
                |> List.filter (fun m ->
                    (DateTime.UnixEpoch.AddSeconds(float m.StartTime)) >= since)
            | None -> []
    }
    
let getPlayer accountId =
    async {
        let path = $"/players/%s{accountId}"
        let request = RestRequest(path)
        
        return execute<Player> request
    }