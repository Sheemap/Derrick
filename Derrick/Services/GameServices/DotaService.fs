module Derrick.Services.GameServices.DotaService

open System
open DataTypes
open Derrick.Services
open Derrick.Services.GameApiClients.OpenDotaResponses
open Derrick.Services.GameApiClients.OpenDotaClient
open Derrick.Services.GameServices.DotaTypes

let fetchMatches sinceDate accounts : seq<PlayerMatch> =
    let unFlattenedMatches =
        accounts
        |> List.map (getPlayerMatches sinceDate)
        |> Async.Parallel
        |> Async.RunSynchronously
        
    seq{
        for matchList in unFlattenedMatches do
            for gameMatch in matchList do
                yield gameMatch
    }
    
let foldMatches playerMatches =
    let initial = { Kills = List.empty; Deaths = List.empty; Assists = List.empty; Gpm = List.empty; ObserversPurchased = List.empty;
        Xpm = List.empty; HeroDamage = List.empty; TowerDamage = List.empty; LastHits = List.empty; HeroHealing = List.empty; SentriesPurchased = List.empty }
    
    if Seq.length playerMatches > 0 then
        Some (Seq.fold (fun acc (curr:PlayerMatch) ->
            { Kills = List.append acc.Kills [curr.Kills]
              Deaths = List.append acc.Deaths [curr.Deaths]
              Assists = List.append acc.Assists [curr.Assists]
              Gpm = List.append acc.Gpm [curr.Gpm]
              Xpm = List.append acc.Xpm [curr.Xpm]
              HeroDamage = List.append acc.HeroDamage [curr.HeroDamage]
              TowerDamage = List.append acc.TowerDamage [curr.TowerDamage]
              LastHits = List.append acc.LastHits [curr.LastHits]
              HeroHealing = List.append acc.HeroHealing [curr.HeroHealing]
              ObserversPurchased = List.append acc.ObserversPurchased [curr.PurchaseWardObserver]
              SentriesPurchased = List.append acc.SentriesPurchased [curr.PurchaseWardSentry] })
            initial playerMatches)
    else
        None
    
let loadStats sinceDate users =
        users
        |> List.map (fun (user:uint64 * GameAccount list) ->
            let accountIds = List.map (fun u -> u.LinkedId) (snd user)
            (fst user,
             fetchMatches sinceDate accountIds
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
          BigBrainAward(0).Award playerStats
          BruiserAward(0).Award playerStats
          SerialKillerAward(0).Award playerStats
          AccompliceAward(0).Award playerStats
          BulldozerAward(0).Award playerStats
          HumbleFarmerAward(0).Award playerStats
          EThotAward(0).Award playerStats
          OmnipotentAward(0).Award playerStats
          FeederAward(0).Award playerStats ]
    else
        []
    