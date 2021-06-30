namespace Derrick.Modules

open DSharpPlus.Entities
open DSharpPlus
open System.Collections.Generic
open Chessie.ErrorHandling
open Derrick.Shared
open Derrick.Services

module Setup =
    let [<Literal>] commandName = "setup"
    let channelOptName = "channel"
    let gameOptName = "game"
    let scheduleOptName = "schedule"

    let command =
        let channelOpt =
            DiscordApplicationCommandOption(channelOptName, "Channel for sending awards", ApplicationCommandOptionType.Channel, true)

        let dotaChoice =
            DiscordApplicationCommandOptionChoice("Dota", int Games.Dota)

        let leagueChoice =
            DiscordApplicationCommandOptionChoice("League", int Games.League)

        let gameOpt =
            DiscordApplicationCommandOption(gameOptName, "Game to send awards for", ApplicationCommandOptionType.Integer, true, [ dotaChoice; leagueChoice ])

        let timeOpt =
            DiscordApplicationCommandOption(scheduleOptName, "Cron schedule for when to send", ApplicationCommandOptionType.String, false)

        DiscordApplicationCommand(commandName, "Configure game awards to send on a channel", [ channelOpt; gameOpt; timeOpt ])

    let channelIdValue (opts:DiscordInteractionDataOption seq) =
        let opt = Seq.find (fun (a:DiscordInteractionDataOption) -> a.Name = channelOptName) opts
        System.Convert.ToUInt64(opt.Value)

    let gameValue (opts:DiscordInteractionDataOption seq) =
        let opt = Seq.find (fun (a:DiscordInteractionDataOption) -> a.Name = gameOptName) opts
        let optInt = System.Convert.ToInt32(opt.Value)
        enum<Games> optInt

    let scheduleValue (opts:DiscordInteractionDataOption seq) =
        let defaultCronSchedule = CronSchedule.create("0 0 * * 2")
        try
        let opt = Seq.find (fun (a:DiscordInteractionDataOption) -> a.Name = scheduleOptName) opts
        let optString = System.Convert.ToString(opt.Value)
        CronSchedule.create(optString)
        with
        | :? KeyNotFoundException -> defaultCronSchedule

    // Finds the maximum frequency of a cron schedule over a given date range
    let rec maxFrequency (schedule:CronSchedule.CronSchedule) (starting:System.DateTime) ending breakpoint =
        if starting >= ending then
            System.TimeSpan.MinValue
        else
            let next = schedule.NextTime(starting.AddMinutes(1.0))
            match next with
            | Some someTime ->
                let followup = schedule.NextTime(someTime.AddMinutes(1.0))
                match followup with
                | Some someFollowup ->

                    let freq = someFollowup.Subtract(someTime)
                    if freq < breakpoint then freq
                    else
                        let nextFreq = maxFrequency schedule someFollowup ending breakpoint
                        if freq > nextFreq then freq
                        else nextFreq

                | None -> System.TimeSpan.MinValue
            | None -> System.TimeSpan.MinValue

    let cronFrequency (schedule:CronSchedule.CronSchedule) =
        maxFrequency schedule System.DateTime.Now (System.DateTime.Now.AddYears 1) (System.TimeSpan.FromHours 24.0)

    type Request =
        { Channel : DiscordChannel
          Game : Games
          Schedule : CronSchedule.CronSchedule
          Interaction : DiscordInteraction }

    let validatePermission request =
        let guildMember =
            request.Interaction.User.Id
            |> request.Interaction.Guild.GetMemberAsync
            |> Async.AwaitTask
            |> Async.RunSynchronously
        if (isNull guildMember) then
            fail "Error validating permission. Please try again"
        else
            let perms = request.Channel.PermissionsFor(guildMember)
            if perms.HasPermission Permissions.ManageChannels then
                ok request
            else
                fail "Permission denied. You must have the 'Manage Channel' permission to do this!"
    
    let validateParse request =
        if not (System.String.IsNullOrWhiteSpace(request.Schedule.fail)) then fail request.Schedule.fail
        else ok request

    let validateFrequency request =
        let frequency = cronFrequency request.Schedule
        if frequency < System.TimeSpan.FromHours(24.0) then fail "I cant send a message more than once every 24 hours!"
        else ok request

    let validateTextChannel request =
        if not (request.Channel.Type = ChannelType.Text) then fail "Can only setup in a text channel!"
        else ok request

    let validateNewConfig request =
        let config = DataService.getConfig (request.Channel.Id, request.Game)
        match config with
        | Some _ -> fail "This configuration already exists!"
        | None -> ok request

    let validateCanSend request =
        let perms = request.Channel.PermissionsFor(request.Interaction.Guild.CurrentMember)
        if perms.HasPermission(Permissions.AccessChannels) &&
           perms.HasPermission(Permissions.SendMessages)
            then ok request
        else fail "I don't have permission to send to that channel!"

    let checkManageChannel request =
        let perms = request.Channel.PermissionsFor(request.Interaction.Guild.CurrentMember)
        if perms.HasPermission(Permissions.ManageChannels) then ok request
        else warn "I don't have the Manage Channel permission on this channel.\n\
                  This means I wont be able to add users automatically if the channel is hidden, they will have to be added manually.\n\
                  Grant me this permission to resolve this warning\n\
                  \nIf the channel is not hidden, you may safely ignore this warning." request

    let insertRecord request =
        try
        ignore(DataService.addConfig request.Channel.Id request.Game request.Schedule request.Interaction.User.Id)
        ok request
        with
        | _ -> fail "Internal error occured! Please contact Seka"

    let executeRequest =
        validatePermission
        >> bind validateParse
        >> bind validateFrequency
        >> bind validateTextChannel
        >> bind validateNewConfig
        >> bind validateCanSend
        >> bind checkManageChannel
        >> bind insertRecord

    let handler (client:DiscordClient) (interaction:DiscordInteraction) =
        let channelId = channelIdValue interaction.Data.Options
        let game = gameValue interaction.Data.Options
        let schedule = scheduleValue interaction.Data.Options

        let channel = client.GetChannelAsync(channelId) |> Async.AwaitTask |> Async.RunSynchronously

        let request = {Channel = channel; Game = game; Schedule = schedule; Interaction=interaction}

        match (executeRequest request) with
        | Fail err ->
            let errString = String.concat "\n\n:no_entry_sign: " err
            Shared.updateInteractionResponse interaction $":no_entry_sign: Error(s): %s{errString}"
        | Warn (_, warn) ->
            let warnString = String.concat "\n\n:warning:" warn
            Shared.updateInteractionResponse interaction $":white_check_mark: Channel config added!\n\n:warning: Warning(s): %s{warnString}"
        | Pass _ -> Shared.updateInteractionResponse interaction ":white_check_mark: Channel config added!"