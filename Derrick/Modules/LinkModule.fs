namespace Derrick.Modules

open DSharpPlus.Entities
open DSharpPlus
open Chessie.ErrorHandling
open Derrick.Services.GameApiClients
open Derrick.Shared
open Derrick.Services

module Link =
    let [<Literal>] commandName = "link"
    let accountOptName = "account_id"
    let gameOptName = "game"

    let command =
        let accountOpt =
            DiscordApplicationCommandOption(accountOptName, "Game Account ID", ApplicationCommandOptionType.String, true)

        let dotaChoice =
            DiscordApplicationCommandOptionChoice("Dota", int Games.Dota)

        let leagueChoice =
            DiscordApplicationCommandOptionChoice("League", int Games.League)

        let gameOpt =
            DiscordApplicationCommandOption(gameOptName, "Game to register for", ApplicationCommandOptionType.Integer, true, [ dotaChoice; leagueChoice ])

        DiscordApplicationCommand(commandName, "Link a game account to your discord account", [ gameOpt; accountOpt ], true)

    let accountIdValue (opts:DiscordInteractionDataOption seq) =
        let opt = Seq.find (fun (a:DiscordInteractionDataOption) -> a.Name = accountOptName) opts
        string opt.Value

    let gameValue (opts:DiscordInteractionDataOption seq) =
        let opt = Seq.find (fun (a:DiscordInteractionDataOption) -> a.Name = gameOptName) opts
        let optInt = System.Convert.ToInt32(opt.Value)
        enum<Games> optInt

    type Request =
        { AccountId : string
          Game : Games
          Interaction : DiscordInteraction }

    let validateAccountId request =
        let exists =
            match request.Game with
            | Games.Dota ->
                OpenDotaClient.getPlayer request.AccountId
                |> Async.RunSynchronously
                |> Option.isSome
            | Games.League ->
                LeagueClient.getSummonerByPuuid request.AccountId
                |> Async.RunSynchronously
                |> isNull
                |> not
            | _ -> false
            
        
        if exists then ok request
        else fail "Account not found! Is your ID correct?"

    let validateAccountNotClaimed request =
        match DataService.getLinkedAccount (request.AccountId, request.Game) with
        | Some _ -> fail "Account is already claimed!"
        | None -> ok request
        
    let insertLink request =
        let discordId = request.Interaction.User.Id
        if DataService.addLinkedAccount request.AccountId discordId request.Game discordId > 0 then
            ok request
        else
            fail "Error occurred while adding link! Please alert Seka."

    let executeRequest =
        validateAccountNotClaimed
        >> bind validateAccountId
        >> bind insertLink

    let handler (client:DiscordClient) (interaction:DiscordInteraction) =
        let accountId = accountIdValue interaction.Data.Options
        let game = gameValue interaction.Data.Options
        
        let request = { AccountId = accountId; Game = game; Interaction=interaction }

        match (executeRequest request) with
        | Fail err ->
            let errString = String.concat "\n\n:no_entry_sign: " err
            Shared.updateInteractionResponse interaction $":no_entry_sign: Error(s): %s{errString}"
        | Warn (_, warn) ->
            let warnString = String.concat "\n\n:warning:" warn
            Shared.updateInteractionResponse interaction $":white_check_mark: Account registered!\n\n:warning: Warning(s): %s{warnString}"
        | Pass _ -> Shared.updateInteractionResponse interaction ":white_check_mark: Account registered!"