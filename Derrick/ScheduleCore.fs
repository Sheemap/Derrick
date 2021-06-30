module Derrick.ScheduleCore

open System.Threading
open DSharpPlus.Entities
open DataTypes
open System
open Derrick.Shared
open Derrick.Services
open Derrick.Services.GameServices
open Serilog

let Adjectives =
    [ "an awesome"; "a fantastic"; "a delicious"; "a delightful"; "a scrumptious"; "a boolin"; "a sick"; "a rad"; "a tubular"; "a cool"; "a nice"; "a super"; "a neato"; "an okayish"; "an alright"; "a fine"; "a decent"; "a mediocre"; "a chill"; "an amazing" ]
  
let Exclamations =
    [ "Wow!"; "***WOW!***"; "Waow."; "Nice."; "Neat."; "Neato."; "Cool."; "Cool!"; "***RAD!***"; "Dope."; "Dope!"; "Slick."; "Chill."; "Not bad."; "Eh."; "Super." ]

let msgGen score =
    let adj = Adjectives.[Random().Next(0, (List.length Adjectives) - 1)]
    let excl = Exclamations.[Random().Next(0, (List.length Exclamations) - 1)]
    $"<@%i{score.Id}> had %s{adj} **%.2f{score.Avg}** average with a max of **%i{score.Max}** over **%i{score.Count}** games! %s{excl}"

let saveAwardHistory (config:ChannelConfig) awards =
        awards
        |> List.map (fun a -> (a, config.ChannelId, config.Game))
        |> DataService.addAwardHistory
        |> ignore
    
    
let processConfig (config:ChannelConfig) =
    let awards =
        match config.Game with
        | Games.Dota -> DotaService.generateAwards config
        | Games.League -> []
        | _ -> []
    
    saveAwardHistory config awards
    awards

let buildAwardMessages award =
    DiscordEmbedBuilder()
        .WithColor(award.Color)
        .WithAuthor(name = award.Subject, iconUrl = award.IconUrl)
        .AddField(award.Name, msgGen award.Score)
        .Build()

let loop () =
    while true do
        try
        let toProcess =
            DataService.getAllConfigs ()
            |> List.filter (fun c ->
                c.Schedule.NextTime(defaultArg c.LastSentUTC c.DateCreatedUTC) <= Some DateTime.UtcNow)
        
        
        for config in toProcess do
            Log.Information("Processing config. ChannelId: {ChannelId}. Game: {Game}", config.ChannelId, config.Game)
            let awards = processConfig config
            let channel = BotCore.discord.GetChannelAsync config.ChannelId |> Async.AwaitTask |> Async.RunSynchronously
            
            if List.length awards > 0 then
                let awardMessages =
                    awards
                    |> List.map buildAwardMessages
                
                for msg in awardMessages do
                    channel.SendMessageAsync msg
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                    |> ignore
            else
                channel.SendMessageAsync "No awards this time! Looks like no one played :)" |> Async.AwaitTask |> Async.RunSynchronously |> ignore
                
            DataService.updateLastRun (config.ChannelId, config.Game) |> ignore
        
        Thread.Sleep(30000)
        with
        | exn -> Log.Error(exn, "Exception in main loop occurred!")