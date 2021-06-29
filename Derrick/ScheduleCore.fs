module Derrick.ScheduleCore

open System.Threading
open DSharpPlus.Entities
open DataTypes
open System
open Derrick.Shared
open Derrick.Services
open Derrick.Services.GameServices


let processConfig (config:ChannelConfig) =
    //Logic to send message and what not
    match config.Game with
    | Games.Dota -> DotaService.generateAwards config
    | Games.League -> []
    | _ -> []
    

let buildAwardMessages award =
    DiscordEmbedBuilder()
        .WithColor(award.Color)
        .WithAuthor(name = award.Subject, iconUrl = award.IconUrl)
        .AddField(award.Name, award.Message)
        .Build()

let loop =
    while true do
        let toProcess =
            DataService.getAllConfigs ()
            |> List.filter (fun c ->
                c.Schedule.NextTime(defaultArg c.LastSentUTC c.DateCreatedUTC) <= Some DateTime.UtcNow)
        
        for config in toProcess do
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