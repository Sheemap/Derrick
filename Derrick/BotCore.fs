namespace Derrick

open System
open System.Threading.Tasks
open DSharpPlus
open DSharpPlus.Entities
open DSharpPlus.EventArgs
open DataTypes
open Emzi0767.Utilities
open Derrick.Shared
open Microsoft.Extensions.Logging
open Derrick.Modules
open Derrick.Services
open Chessie.ErrorHandling

module BotCore =
    let dconf = DiscordConfiguration()
    dconf.set_AutoReconnect true
    dconf.set_Token (getEnvValueOrThrow "DERRICK_DISCORD_TOKEN")
    dconf.set_TokenType TokenType.Bot
    dconf.MinimumLogLevel <- LogLevel.Information

    let discord = new DiscordClient(dconf)

    let readyHandler (client: DiscordClient) (args: ReadyEventArgs) =
        printfn $"Ready! Logged in as %s{client.CurrentUser.Username}#%s{client.CurrentUser.Discriminator}"
        
        discord.BulkOverwriteGlobalApplicationCommandsAsync([ Setup.command; Join.command ])
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore

        Task.CompletedTask

    type Request =
        { ButtonData : ButtonData
          CurrentInteraction : DiscordInteraction }
    
    // Returns bool based on whether we should reply or not
    let validateSpecificUser request =
        match request.ButtonData.UniqueUser with
        | Some id ->
            if request.CurrentInteraction.User.Id = id then ok request
            else fail $"<@%i{request.CurrentInteraction.User.Id}>, that wasn't meant for you :)"
        | None -> ok request
                   
    
    let acknowledgeInteraction (interaction:DiscordInteraction) =
        interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    
    let cleanupInteraction (interaction: DiscordInteraction) =
        interaction.DeleteOriginalResponseAsync() |> Async.AwaitTask |> Async.RunSynchronously
    
    let interactionHandler (client: DiscordClient) (interactionEvent: InteractionCreateEventArgs) : Task =
        acknowledgeInteraction interactionEvent.Interaction
        addCacheItem
            (string interactionEvent.Interaction.Id)
            interactionEvent.Interaction
            (AbsoluteExpiration (DateTimeOffset.Now.AddMinutes(14.0)))
            (cacheItemCallback<DiscordInteraction> cleanupInteraction)
        |> ignore
        
        printfn $"Interaction received. Command name: %s{interactionEvent.Interaction.Data.Name}"

        match interactionEvent.Interaction.Data.Name with
        | Setup.commandName -> Setup.handler client interactionEvent.Interaction
        | Join.commandName -> Join.handleInitial client interactionEvent.Interaction
        | Link.commandName -> Link.handler client interactionEvent.Interaction
        | _ -> Shared.updateInteractionResponse interactionEvent.Interaction "Unknown command"

    let componentInteractionHandler (client: DiscordClient) (interactionEvent: InteractionCreateEventArgs) =
        acknowledgeInteraction interactionEvent.Interaction
        addCacheItem
            (string interactionEvent.Interaction.Id)
            interactionEvent.Interaction
            (AbsoluteExpiration (DateTimeOffset.Now.AddMinutes(14.0)))
            (cacheItemCallback<DiscordInteraction> cleanupInteraction)
        |> ignore
        
        printfn $"Interaction received. Custom Id: %s{interactionEvent.Interaction.Data.CustomId}"
        
        let buttonInteraction =
            DataService.getButtonData interactionEvent.Interaction.Data.CustomId
            
        match buttonInteraction with
        | None -> Shared.updateInteractionResponse interactionEvent.Interaction "Unknown command"
        | Some buttonInteraction -> 
                match validateSpecificUser {ButtonData = buttonInteraction; CurrentInteraction = interactionEvent.Interaction} with
                | Fail err ->
                    let errString = String.concat ", " err
                    Shared.updateInteractionResponse interactionEvent.Interaction errString
                | Pass _ ->
                    match buttonInteraction.CommandModule with
                    | Join.commandName -> Join.handleButtonInteraction client interactionEvent.Interaction buttonInteraction
                    | _ -> Shared.updateInteractionResponse interactionEvent.Interaction "Unknown command"
                | Warn (_, _) -> Task.CompletedTask //Unused right now


    discord.add_Ready (AsyncEventHandler<DiscordClient, ReadyEventArgs>(readyHandler))
    discord.add_InteractionCreated (AsyncEventHandler<DiscordClient, InteractionCreateEventArgs>(interactionHandler))
    discord.add_ComponentInteractionCreated(AsyncEventHandler<DiscordClient, ComponentInteractionCreateEventArgs>(componentInteractionHandler))