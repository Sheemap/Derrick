namespace Derrick.Modules

open System
open ButtonService
open DSharpPlus.Entities
open DSharpPlus
open DataTypes
open System.Threading.Tasks
open Chessie.ErrorHandling
open Derrick.Shared
open Derrick.Services

module Join =    
    let [<Literal>] commandName = "join"

    let command =
        DiscordApplicationCommand(commandName, "Register for game awards")
       
    type Request =
        { Interaction : DiscordInteraction }
        
    let validateNonDM request =
        if request.Interaction.Guild = null then fail "Command can't be used within a DM"
        else ok request

    let channelButtonPrompt (channels:DiscordChannel seq) interaction =
        let buttonDataList =
            channels
            |> Seq.toList
            |> List.map (fun channel ->
               { ButtonData =
                     { Id = Guid.NewGuid()
                       CommandModule = commandName
                       ParentInteraction = Some interaction
                       UniqueUser = Some interaction.User.Id
                       Data = channel.Id }
                 ButtonStyle = ButtonStyle.Primary
                 ButtonTitle = $"#%s{channel.Name}"
               })
            |> genButtonComponents
                        
        let res = DiscordWebhookBuilder()
        res.Content <- "Which channel would you like to join?"
        res.AddComponents(buttonDataList)

    type GameButtonData =
        { Game: Games
          ChannelId: uint64}
    
    let gameButtonPrompt (configs:seq<ChannelConfig>) interaction =
        let buttonDataList =
            configs
            |> Seq.toList
            |> List.map (fun config ->
               { ButtonData =
                     { Id = Guid.NewGuid()
                       CommandModule = commandName
                       ParentInteraction = Some interaction
                       UniqueUser = Some interaction.User.Id
                       Data = { Game = config.Game; ChannelId = config.ChannelId } }
                 ButtonStyle = ButtonStyle.Primary
                 ButtonTitle = config.Game.ToString()
               })
            |> genButtonComponents
        
        let res = DiscordWebhookBuilder()
        res.Content <- "Which game would you like to register for?"
        res.AddComponents(buttonDataList)

    let handleGameChoice (config:ChannelConfig) (interaction:DiscordInteraction) =
        DataService.addConfigUser config.ChannelId config.Game interaction.User.Id |> ignore
        
        let addToChannelTask =
            let channelSuccess, channel = interaction.Guild.Channels.TryGetValue config.ChannelId
            if channelSuccess then
                let interactionMember = interaction.Guild.GetMemberAsync interaction.User.Id |> Async.AwaitTask |> Async.RunSynchronously
                let perms = channel.PermissionsFor(interaction.Guild.CurrentMember)
                if perms.HasPermission(Permissions.ManageChannels) then
                    channel.AddOverwriteAsync(interactionMember, Permissions.AccessChannels)
                else Task.CompletedTask
            else Task.CompletedTask
        
        Task.WhenAll [addToChannelTask; Shared.updateInteractionResponse interaction "Joined successfully!"]
        
    let handleChannelChoice (configs:ChannelConfig list) (userConfigs:ChannelRegistration list)interaction =
        let filteredConfigs =
            configs
            |> List.filter (fun c -> (List.tryFind (fun (uc:ChannelRegistration) -> uc.ChannelId = c.ChannelId && uc.Game = c.Game) userConfigs) = None)
        
        match Seq.length filteredConfigs with
        | c when c < 1 -> // There should never be no config at this point, bad
            Shared.updateInteractionResponse interaction $":no_entry_sign: Error occured! This shouldn't have happened! Please contact Seka."
        | c when c = 1 -> // Handle the only game
            handleGameChoice filteredConfigs.[0] interaction
        | _ -> // Prompt for game choice
            let res = gameButtonPrompt configs interaction
            interaction.EditOriginalResponseAsync(res) :> Task


    let handleInitial (client:DiscordClient) (interaction:DiscordInteraction) : Task =
        let req = {Interaction = interaction}
                        
        match validateNonDM req with
        | Fail err ->
            let errString = String.concat "\n\n:no_entry_sign: " err
            Shared.updateInteractionResponse interaction $":no_entry_sign: Error(s): %s{errString}"
        | _ ->
            let userConfigs = DataService.getConfigUsers(interaction.User.Id)
            let configs = interaction.Guild.Channels
                            |> Seq.filter (fun c -> c.Value.Type = ChannelType.Text)
                            |> Seq.map (fun c -> c.Key)
                            |> DataService.getConfigs
                            |> List.filter (fun c -> (List.tryFind (fun (uc:ChannelRegistration) -> uc.ChannelId = c.ChannelId && uc.Game = c.Game) userConfigs) = None)

            let distinctConfigs = Seq.distinctBy (fun (c:ChannelConfig) -> c.ChannelId) configs

            let configChanIds = Seq.map (fun (c:ChannelConfig) -> c.ChannelId) distinctConfigs
            let channels = interaction.Guild.Channels
                            |> Seq.map (fun c -> c.Value)
                            |> Seq.filter (fun (c:DiscordChannel) -> Seq.contains c.Id configChanIds)

            match Seq.length distinctConfigs with
            | c when c < 1 -> // No configs found for this user
               Shared.updateInteractionResponse interaction $"I couldn't find anything to join :sob: Either you've joined all the available configs, or a server admin needs to set one up."
            | c when c = 1 -> // Handle the only channel
                handleChannelChoice configs userConfigs interaction
            | _ -> // Prompt for channel choice
                let res = channelButtonPrompt channels interaction
                interaction.EditOriginalResponseAsync(res) :> Task
    
    let handleButtonInteraction (client:DiscordClient) (interaction:DiscordInteraction) dbButtonData =
        let success, value = UInt64.TryParse dbButtonData.JsonData
        if success then
            let buttonData = getDeserializedButtonData<uint64> dbButtonData
            let deleteTask = Shared.deleteInteraction buttonData.ParentInteraction
            
            let chanChoiceTask = handleChannelChoice (DataService.getConfigs [value]) (DataService.getConfigUsers interaction.User.Id) interaction
            Task.WhenAll [ deleteTask; chanChoiceTask ]
        else
            let buttonData = getDeserializedButtonData<GameButtonData> dbButtonData
            let deleteTask = Shared.deleteInteraction buttonData.ParentInteraction
            
            let configOption = DataService.getConfig (buttonData.Data.ChannelId, buttonData.Data.Game)
            match configOption with
            | None ->
                Task.WhenAll [ Shared.updateInteractionResponse interaction $":no_entry_sign: Error occured! This shouldn't have happened! Please contact Seka."
                               deleteTask ]
            | Some c ->
                Task.WhenAll [ deleteTask; handleGameChoice c interaction ]