module Derrick.Services.GameServices.DotaService

open System
open DataTypes
open Derrick.Services
open Derrick.Services.GameApiClients.OpenDotaResponses
open Derrick.Services.GameApiClients.OpenDotaClient
open Derrick.Services.GameServices.DotaTypes

let foldMatches playerMatches =
    let initial = { Kills = List.empty; Deaths = List.empty; Assists = List.empty; Gpm = List.empty; ObserversPurchased = List.empty;
        Xpm = List.empty; HeroDamage = List.empty; TowerDamage = List.empty; LastHits = List.empty; HeroHealing = List.empty; SentriesPurchased = List.empty }
    
    if Seq.length playerMatches > 0 then
        Some (Seq.fold (fun acc (curr:PlayerMatch) ->
            let obsPurchased =
                if curr.PurchaseWardObserver.HasValue
                then curr.PurchaseWardObserver.Value
                else 0
                
            let sentPurchased =
                if curr.PurchaseWardSentry.HasValue
                then curr.PurchaseWardSentry.Value
                else 0
            
            { Kills = List.append acc.Kills [curr.Kills]
              Deaths = List.append acc.Deaths [curr.Deaths]
              Assists = List.append acc.Assists [curr.Assists]
              Gpm = List.append acc.Gpm [curr.Gpm]
              Xpm = List.append acc.Xpm [curr.Xpm]
              HeroDamage = List.append acc.HeroDamage [curr.HeroDamage]
              TowerDamage = List.append acc.TowerDamage [curr.TowerDamage]
              LastHits = List.append acc.LastHits [curr.LastHits]
              HeroHealing = List.append acc.HeroHealing [curr.HeroHealing]
              ObserversPurchased = List.append acc.ObserversPurchased [obsPurchased]
              SentriesPurchased = List.append acc.SentriesPurchased [sentPurchased] })
            initial playerMatches)
    else
        None

// THIS MUST BE THE ONLY UNPURE FUNCTION
// PIPELINE EVERYTHING, ALL UNPURE OPERATIONS FOR DOTA TAKE PLACE HERE
let generateAwards config activeDiscordIds =
    let sinceDate = defaultArg config.LastSentUTC config.DateCreatedUTC
    
    let playerStats =
        DataService.getGameAccounts (config.ChannelId, config.Game) //UNPURE
        |> List.filter (fun x -> (Seq.contains x.DiscordId activeDiscordIds))
        |> List.map (fun acc ->
            (acc.DiscordId,
             getPlayerMatches sinceDate acc.LinkedId //UNPURE
                |> Async.RunSynchronously
                |> foldMatches))
        |> List.filter (fun (_, data) -> data.IsSome)
        |> List.map (fun (user, data) -> (user, Option.get data))
    
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
    