module DataTypes

open System
open Derrick.Shared

type ChannelConfig =
    { ChannelId: uint64
      Game: Games
      Schedule: CronSchedule.CronSchedule
      LastSentUTC: DateTime option
      DateCreatedUTC: DateTime
      CreatedBy: uint64
      DateUpdatedUTC: DateTime
      UpdatedBy: uint64 }

type ChannelRegistration =
    { ChannelId: uint64 
      Game: Games
      UserId: uint64
      DateCreatedUTC: DateTime
      CreatedBy: uint64 }
    
type ButtonData =
    { Id: Guid
      CommandModule: string
      ParentInteractionId: uint64
      UniqueUser: uint64 option
      JsonData: string
      DateCreatedUTC: DateTime }
    
type GameAccount =
    { LinkedId: string
      DiscordId: uint64
      Game: Games
      DateCreatedUTC: DateTime
      CreatedBy: uint64 }
    
type AwardHistory =
    { Id: int
      AwardType: AwardType
      WinnerId: uint64
      ChannelId: uint64
      Game: Games
      Average: float
      Max: int
      Count: int }
    
type Match<'T> =
    { Id: string
      Game: Games
      Data: 'T
      DatePlayedUTC: DateTime }
    
type MatchPlayer =
    { MatchId: string
      PlayerId: string
      Game: Games }