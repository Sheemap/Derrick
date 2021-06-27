module Derrick.Services.GameApiClients.OpenDotaClient

open System
open System.Collections.Generic
open System.Threading
open DSharpPlus.Exceptions
open Newtonsoft.Json
open RestSharp
open Derrick.Services.GameApiClients.OpenDotaResponses

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

let updateRateLimit (response:IRestResponse) =
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
        
let handleErrorsReturnData<'T> (response:IRestResponse) =
    JsonConvert.DeserializeObject<'T>(response.Content)
    
let execute<'T> request =
    if monthRateLimit < 50 then
        raise (Exception "Monthly OpenDota rate limit exceeded!")
        
    Thread.Sleep getApiDelay
    
    let client = RestClient(ApiUrl)

    let response = client.Execute(request)
        
    updateRateLimit response
    
    handleErrorsReturnData<'T> response

let getPlayerMatches (since:DateTime) accountId  =
    async {
        let path = $"/players/%s{accountId}/matches"
        let request = RestRequest(path)
        
        let daysPrevious = (DateTime.Now - since).Days + 1
        
        request.AddQueryParameter ("date", (string daysPrevious)) |> ignore
        for item in ProjectionInfo do
            request.AddQueryParameter ("project", item) |> ignore
                
        return execute<PlayerMatch list> request
            |> List.filter (fun m ->
                (DateTime.UnixEpoch.AddSeconds(float m.StartTime)) >= since)
    }