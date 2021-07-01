namespace Derrick.Modules

open DSharpPlus.Entities
open System.Threading.Tasks

module Shared =

    let updateInteractionResponse (interaction:DiscordInteraction) (message:string)  =
        let res = DiscordWebhookBuilder()
        res.Content <- message
        interaction.EditOriginalResponseAsync(res) :> Task
        
    let sendFollowupInteraction (interaction:DiscordInteraction) (message:string)  =
        let res = DiscordFollowupMessageBuilder()
        res.Content <- message
        interaction.CreateFollowupMessageAsync(res) :> Task
                
    let deleteInteraction (interaction:DiscordInteraction option) =
        match interaction with
        | None -> Task.CompletedTask
        | Some i -> i.DeleteOriginalResponseAsync()