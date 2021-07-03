module Derrick.Services.GameApiClients.LeagueClient

open RiotSharp

let api = RiotApi.GetDevelopmentInstance("INVALID");

let private toColdAsync task =
    async {
        return task |> Async.AwaitTask |> Async.RunSynchronously
    }

let getSummonerByPuuid name =
    toColdAsync (api.Summoner.GetSummonerByPuuidAsync(Misc.Region.Na, name))
    
let getSummonerHistory puuid =
    toColdAsync (api.Match.GetMatchListAsync(Misc.Region.Americas, puuid))
    
let getMatch matchId =
    api.Match.GetMatchAsync(Misc.Region.Americas, matchId) |> Async.AwaitTask |> Async.RunSynchronously