module Derrick.Services.DataService

open Derrick.Shared
open Npgsql.FSharp
open DataTypes

//TODO: Load settings from somewhere external (env probably)
let host = "10.0.0.169"
let username = "INVALID"
let password ="INVALID"
let dbName = "WOTW"
let portNum = 5432

let connectionString : string =
    Sql.host host
    |> Sql.database dbName
    |> Sql.username username
    |> Sql.password password
    |> Sql.port portNum
    |> Sql.formatConnectionString

let getConfig (channelId:uint64, game:Games) : ChannelConfig option =
    let parsedChan = int64 channelId
    let gameId = int game

    try
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT * FROM public.channel_config WHERE channel_id = @channel_id AND game = @game LIMIT 1"
    |> Sql.parameters ["@channel_id", Sql.int64 parsedChan; "@game", Sql.int gameId ]
    |> Sql.executeRow (fun read ->
        {
            ChannelId = uint64 (read.int64 "channel_id")
            Game = enum<Games>(read.int "game")
            Schedule = CronSchedule.create(read.text "schedule")
            LastSentUTC = read.dateTimeOrNone "last_sent_utc"
            DateCreatedUTC = read.dateTime "date_created_utc"
            CreatedBy = uint64 (read.int64 "created_by")
            DateUpdatedUTC = read.dateTime "date_updated_utc"
            UpdatedBy = uint64 (read.int64 "updated_by")
        })
    |> Some
    with
    | :? NoResultsException -> None

let addConfig (channelId:uint64) (game:Games) (cronSchedule:CronSchedule.CronSchedule) (userId:uint64) : int =
    let parsedChan = int64 channelId
    let gameId = int game
    let parsedUser = int64 userId

    connectionString
    |> Sql.connect
    |> Sql.query "INSERT INTO channel_config
                    (channel_id, game, schedule, date_created_utc, created_by, date_updated_utc, updated_by)
                  VALUES
                    (@channel_id, @game, @schedule, @date, @userId, @date, @userId)"
    |> Sql.parameters [ "@channel_id", Sql.int64 parsedChan
                        "@game", Sql.int gameId
                        "@schedule", Sql.text cronSchedule.original
                        "@date", Sql.timestamp System.DateTime.UtcNow
                        "@userId", Sql.int64 parsedUser
                        ]
    |> Sql.executeNonQuery

let getAllConfigs () =
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT * FROM channel_config"
    |> Sql.execute (fun read ->
        {
            ChannelId = uint64 (read.int64 "channel_id")
            Game = enum<Games>(read.int "game")
            Schedule = CronSchedule.create(read.text "schedule")
            LastSentUTC = read.dateTimeOrNone "last_sent_utc"
            DateCreatedUTC = read.dateTime "date_created_utc"
            CreatedBy = uint64 (read.int64 "created_by")
            DateUpdatedUTC = read.dateTime "date_updated_utc"
            UpdatedBy = uint64 (read.int64 "updated_by")
        })

let getConfigs (channelIds:uint64 seq) : ChannelConfig list =
    let parsedChannels = 
        channelIds
        |> Seq.map int64
        |> Seq.toArray

    connectionString
    |> Sql.connect
    |> Sql.query "SELECT * FROM public.channel_config WHERE channel_id = ANY(@channel_ids)"
    |> Sql.parameters ["@channel_ids", Sql.int64Array parsedChannels ]
    |> Sql.execute (fun read ->
        {
            ChannelId = uint64 (read.int64 "channel_id")
            Game = enum<Games>(read.int "game")
            Schedule = CronSchedule.create(read.text "schedule")
            LastSentUTC = read.dateTimeOrNone "last_sent_utc"
            DateCreatedUTC = read.dateTime "date_created_utc"
            CreatedBy = uint64 (read.int64 "created_by")
            DateUpdatedUTC = read.dateTime "date_updated_utc"
            UpdatedBy = uint64 (read.int64 "updated_by")
        })
    
let updateLastRun (channelId:uint64, game:Games) =
    let parsedChan = int64 channelId
    let gameId = int game
    
    connectionString
    |> Sql.connect
    |> Sql.query "UPDATE channel_config
                  SET last_sent_utc = @last_sent
                  WHERE channel_id = @channelId AND game = @game"
    |> Sql.parameters [ "@channelId", Sql.int64 parsedChan
                        "@game", Sql.int gameId
                        "@last_sent", Sql.timestampOrNone (Some System.DateTime.UtcNow) ]
    |> Sql.executeNonQuery

let addConfigUser (channelId:uint64) (game:Games) (userId:uint64) =
    let parsedChan = int64 channelId
    let gameId = int game
    let parsedUser = int64 userId

    connectionString
    |> Sql.connect
    |> Sql.query "INSERT INTO config_user
                    (channel_id, game, user_id, date_created_utc, created_by)
                  VALUES
                    (@channel_id, @game, @user_id, @date, @user_id)"
    |> Sql.parameters [ "@channel_id", Sql.int64 parsedChan
                        "@game", Sql.int gameId
                        "@date", Sql.timestamp System.DateTime.UtcNow
                        "@user_id", Sql.int64 parsedUser
                        ]
    |> Sql.executeNonQuery
    
let getConfigUsers (userId:uint64) =
    let parsedUserId = int64 userId

    connectionString
    |> Sql.connect
    |> Sql.query "SELECT * FROM public.config_user WHERE user_id = @user_id"
    |> Sql.parameters ["@user_id", Sql.int64 parsedUserId ]
    |> Sql.execute (fun read ->
        {
            ChannelId = uint64 (read.int64 "channel_id")
            Game = enum<Games>(read.int "game")
            UserId = uint64 (read.int64 "user_id")
            DateCreatedUTC = read.dateTime "date_created_utc"
            CreatedBy = uint64 (read.int64 "created_by")
        })
    
let getGameAccounts (channelId:uint64, game:Games) =
    let parsedChan = int64 channelId
    let gameId = int game
    
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT ga.*
                  FROM channel_config cc
                  INNER JOIN config_user cu ON cu.channel_id = cc.channel_id AND cu.game = cc.game
                  INNER JOIN game_accounts ga ON ga.discord_id = cu.user_id AND ga.game = cu.game
                  WHERE cc.channel_id = @channelId AND cc.game = @game"
    |> Sql.parameters [ "@channelId", Sql.int64 parsedChan
                        "@game", Sql.int gameId ]
    |> Sql.execute (fun read ->
        {
            LinkedId = read.string "linked_id"
            Game = enum<Games>(read.int "game")
            DiscordId = uint64 (read.int64 "discord_id")
            DateCreatedUTC = read.dateTime "date_created_utc"
            CreatedBy = uint64 (read.int64 "created_by")
        })

let getParamSeq item =
    let parsedInteractionId = int64 item.ParentInteractionId
    let parsedUserId =
        match item.UniqueUser with
        | Some id -> Some (int64 id)
        | None -> None
    [ "@id", Sql.uuid item.Id
      "@module", Sql.string item.CommandModule
      "@previous_interaction_id", Sql.int64 parsedInteractionId
      "@date_created", Sql.timestamp System.DateTime.UtcNow
      "@data", Sql.string item.JsonData
      "@unique_user", Sql.int64OrNone parsedUserId
    ]

let insertButtonData items =
    let parameters =
        items
        |> List.map getParamSeq
    
    connectionString
    |> Sql.connect
    |> Sql.executeTransaction
           [
           "INSERT INTO button_interactions
              (id, command_module, parent_interaction_id, unique_user, data, date_created_utc)
            VALUES
              (@id, @module, @previous_interaction_id, @unique_user, @data, @date_created)",
              parameters
           ]

let getButtonData (customId:string) =
    let uuid = System.Guid.Parse(customId)
    
    try
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT * FROM button_interactions WHERE id = @id LIMIT 1"
    |> Sql.parameters ["@id", Sql.uuid uuid ]
    |> Sql.executeRow (fun read ->
        {
            Id = read.uuid "id"
            CommandModule = read.string "command_module"
            ParentInteractionId = uint64 (read.int64 "parent_interaction_id")
            UniqueUser = read.int64OrNone "unique_user" |> Option.map uint64
            JsonData = read.string "data"
            DateCreatedUTC = read.dateTime "date_created_utc"
        })
    |> Some
    with
    | :? NoResultsException -> None
    
let getLinkedAccount (accountId:string, game:Games) =
    let gameId = int game
    
    try
    connectionString
    |> Sql.connect
    |> Sql.query "SELECT * FROM game_accounts WHERE linked_id = @accountId AND game = @game LIMIT 1"
    |> Sql.parameters [ "@accountId", Sql.string accountId
                        "@game", Sql.int gameId ]
    |> Sql.executeRow (fun read ->
        {
            LinkedId = read.string "linked_id"
            DiscordId = uint64 (read.int64 "discord_id")
            Game = enum<Games> (read.int "game")
            CreatedBy = uint64 (read.int64 "created_by")
            DateCreatedUTC = read.dateTime "date_created_utc"
        })
    |> Some
    with
    | :? NoResultsException -> None
    
let addLinkedAccount (accountId:string) (discordId:uint64) (game:Games) (createdBy:uint64) =
    let parsedUserId = int64 discordId
    let gameId = int game
    let parsedCreated = int64 createdBy

    connectionString
    |> Sql.connect
    |> Sql.query "INSERT INTO game_accounts
                    (linked_id, discord_id, game, date_created_utc, created_by)
                  VALUES
                    (@linkedId, @discordId, @game, @dateCreatedUtc, @created_by)"
    |> Sql.parameters [ "@linkedId", Sql.string accountId
                        "@discordId", Sql.int64 parsedUserId
                        "@game", Sql.int gameId
                        "@dateCreatedUtc", Sql.timestamp System.DateTime.UtcNow
                        "@created_by", Sql.int64 parsedCreated ]
    |> Sql.executeNonQuery