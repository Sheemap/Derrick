namespace Derrick.Modules

open System
open System.Runtime.Caching
open DSharpPlus.EventArgs
open DSharpPlus.Entities
open DSharpPlus
open System.Threading.Tasks

module Shared =

    let updateInteractionResponse (interaction:DiscordInteraction) (message:string)  =
        let res = DiscordWebhookBuilder()
        res.Content <- message
        interaction.EditOriginalResponseAsync(res) :> Task
        
    let deleteInteraction (interaction:DiscordInteraction option) =
        match interaction with
        | None -> Task.CompletedTask
        | Some i -> i.DeleteOriginalResponseAsync()