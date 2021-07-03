module Derrick.Services.GameServices.LeagueTypes

open Derrick.Shared
open DSharpPlus.Entities

type LeagueUserData =
    { Kills: int list
      Deaths: int list
      Assists: int list
      GoldEarned: int list
      Experience: int list
      LastHits: int list
      ChampDamage: int list
      StructureDamage: int list
      VisionScore: int list }
    
type MidasAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.GoldEarned) threshold playerStats
            
        { Name = "Midas"
          Type = AwardType.LeagueMidas
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(Gold Earned)"
          Score = winningScore }
        
type BruiserAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.ChampDamage) threshold playerStats
            
        { Name = "Bruiser"
          Type = AwardType.LeagueBruiser
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(Damage to Champions)"
          Score = winningScore }
        
type BulldozerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.StructureDamage) threshold playerStats
            
        { Name = "Bulldozer"
          Type = AwardType.LeagueBulldozer
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(Damage to Structures)"
          Score = winningScore }
        
type HumbleFarmerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.LastHits) threshold playerStats
            
        { Name = "Humble Farmer"
          Type = AwardType.LeagueHumbleFarmer
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(CS)"
          Score = winningScore }
        
type SerialKillerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Kills) threshold playerStats
            
        { Name = "Serial Killer"
          Type = AwardType.LeagueSerialKiller
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(Kills)"
          Score = winningScore }
        
type AccompliceAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Assists) threshold playerStats
            
        { Name = "Accomplice"
          Type = AwardType.LeagueAccomplice
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(Assists)"
          Score = winningScore }
        
type OmnipotentAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.VisionScore) threshold playerStats
            
        { Name = "Omnipotent"
          Type = AwardType.LeagueOmnipotent
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(Vision Score)"
          Score = winningScore }
        
type FeederAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Deaths) threshold playerStats
            
        { Name = "Feeder"
          Type = AwardType.LeagueFeeder
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(Deaths)"
          Score = winningScore }