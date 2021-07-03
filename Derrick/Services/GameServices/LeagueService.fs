module Derrick.Services.GameServices.LeagueService

open System
open Chessie.ErrorHandling.AsyncExtensions
open Derrick.Services
open DataTypes
open Derrick.Services.GameApiClients
open Derrick.Services.GameApiClients.LeagueClient
open Derrick.Services.GameServices.LeagueTypes
open Derrick.Shared
open RiotSharp.Endpoints.LeagueEndpoint
open RiotSharp.Endpoints.MatchEndpoint

let formatMatchId platformId gameId =
    platformId + "_" + gameId

let storeMatchesInDb (matches:seq<Match>) =
    let insertedMatches =
        matches
        |> Seq.map (fun m ->
            { Id = formatMatchId m.Info.PlatformId (string m.Info.GameId)
              DatePlayedUTC = m.Info.GameCreation
              Game = Games.League
              Data = m })
        |> Seq.toList
        |> DataService.insertMatches
        
    let insertedPlayers =
            matches
            |> Seq.map (fun m -> (m.Info, m.Info.Participants))
            |> Seq.map (fun (gameMatch, players) ->
                players |>
                Seq.map (fun (p:Participant) ->
                    { MatchId =  formatMatchId gameMatch.PlatformId (string gameMatch.GameId)
                      PlayerId = p.Puuid
                      Game = Games.League }))
            |> Seq.concat
            |> Seq.toList
            |> DataService.insertMatchPlayers
    ()

let fetchMatches sinceDate (linkedIds:string list) =
    async {
        let dbMatches =
            DataService.getMatches<Match> sinceDate linkedIds
            
        let dbMatchIds =
            dbMatches
            |> List.map (fun m -> m.Id)
           
        let summonerHistory =
            Seq.map getSummonerHistory linkedIds
            |> Async.Sequential
            |> Async.RunSynchronously
            
        let apiMatches =
            summonerHistory
            |> Seq.concat
            |> Seq.distinct
            |> Seq.filter (fun id -> not (List.contains id dbMatchIds))
            |> Seq.map getMatch
            |> Seq.toArray
            
        // insert non-db matches
        storeMatchesInDb apiMatches
            
        return
            List.map (fun m -> m.Data) dbMatches
            |> Seq.append apiMatches        
    }
    
let foldMatches playerMatches =
    let initial = { Kills = List.empty; Deaths = List.empty; Assists = List.empty; LastHits = List.empty; ChampDamage = List.empty
                    GoldEarned = List.empty; Experience = List.empty; VisionScore = List.empty; StructureDamage = List.empty }
    
    if Seq.length playerMatches > 0 then
        Some (Seq.fold (fun acc (curr:Participant) ->
            { Kills = List.append acc.Kills [int curr.Kills] 
              Deaths = List.append acc.Deaths [int curr.Deaths]
              Assists = List.append acc.Assists [int curr.Assists]
              GoldEarned = List.append acc.GoldEarned [int curr.GoldEarned]
              Experience = List.append acc.Experience [int curr.ChampExperience]
              VisionScore = List.append acc.VisionScore [int curr.VisionScore]
              ChampDamage = List.append acc.ChampDamage [int curr.TotalDamageDealtToChampions]
              StructureDamage = List.append acc.StructureDamage [int curr.DamageDealtToBuildings]
              LastHits = List.append acc.LastHits [int curr.TotalMinionsKilled] })
            initial playerMatches)
    else
        None    

let loadStats sinceDate users =
        users
        |> List.map (fun (user:uint64 * GameAccount list) ->
            let accountIds = List.map (fun u -> u.LinkedId) (snd user)
            (fst user,
             fetchMatches sinceDate accountIds
             |> Async.RunSynchronously
             |> Seq.map (fun m -> m.Info.Participants)
             |> Seq.concat
             |> Seq.filter (fun p -> Seq.contains p.Puuid accountIds)
             |> foldMatches))
        |> List.filter (fun (_, data) -> data.IsSome)
        |> List.map (fun (user, data) -> (user, Option.get data))

let generateAwards config =
    let sinceDate = (defaultArg config.LastSentUTC (DateTime.UtcNow.AddDays(-7.0)))
    let playerStats =
        DataService.getGameAccounts (config.ChannelId, config.Game)
        |> List.groupBy (fun ga -> ga.DiscordId)
        |> loadStats sinceDate
        
    if List.length playerStats > 0 then
        [ MidasAward(0).Award playerStats
          BruiserAward(0).Award playerStats
          SerialKillerAward(0).Award playerStats
          AccompliceAward(0).Award playerStats
          BulldozerAward(0).Award playerStats
          HumbleFarmerAward(0).Award playerStats
          OmnipotentAward(0).Award playerStats
          FeederAward(0).Award playerStats ]
    else
        []