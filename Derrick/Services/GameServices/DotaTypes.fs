module Derrick.Services.GameServices.DotaTypes

open DSharpPlus.Entities
open Derrick.Shared

type DotaUserData =
    { Kills: int list
      Deaths: int list
      Assists: int list
      Gpm: int list
      Xpm: int list
      HeroDamage: int list
      TowerDamage: int list
      HeroHealing: int list
      LastHits: int list
      ObserversPurchased: int list
      SentriesPurchased: int list }

type MidasAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Gpm) threshold playerStats
            
        { Name = "Midas"
          Type = AwardType.DotaMidas
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(GPM)"
          Score = winningScore }
        
type BigBrainAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Xpm) threshold playerStats
            
        { Name = "Big Brain"
          Type = AwardType.DotaBigBrain
          Color = DiscordColor.Aquamarine
          IconUrl = "https://i.imgur.com/79NQbnw.png"
          Subject = "(XPM)"
          Score = winningScore }
        
type BruiserAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.HeroDamage) threshold playerStats
            
        { Name = "Bruiser"
          Type = AwardType.DotaBruiser
          Color = DiscordColor.Azure
          IconUrl = "https://i.imgur.com/4fSheAx.png"
          Subject = "(Hero Damage)"
          Score = winningScore }
        
type SerialKillerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Kills) threshold playerStats
            
        { Name = "Serial Killer"
          Type = AwardType.DotaSerialKiller
          Color = DiscordColor.DarkRed
          IconUrl = "https://i.imgur.com/6ZH6NxK.png"
          Subject = "(Kills)"
          Score = winningScore }
        
type AccompliceAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Assists) threshold playerStats
            
        { Name = "Accomplice"
          Type = AwardType.DotaAccomplice
          Color = DiscordColor.White
          IconUrl = "https://i.imgur.com/bre8cOp.png"
          Subject = "(Assists)"
          Score = winningScore }
        
type BulldozerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.TowerDamage) threshold playerStats
            
        { Name = "Bulldozer"
          Type = AwardType.DotaBulldozer
          Color = DiscordColor.Red
          IconUrl = "https://i.imgur.com/rGg6IGA.png"
          Subject = "(Structure Damage)"
          Score = winningScore }
        
type HumbleFarmerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.LastHits) threshold playerStats
            
        { Name = "Humble Farmer"
          Type = AwardType.DotaHumbleFarmer
          Color = DiscordColor.Goldenrod
          IconUrl = "https://i.imgur.com/Xb2DnfN.png"
          Subject = "(Last Hits)"
          Score = winningScore }
        
type EThotAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.HeroHealing) threshold playerStats
            
        { Name = "E-Thot"
          Type = AwardType.DotaEThot
          Color = DiscordColor.Green
          IconUrl = "https://i.imgur.com/YPi6w8w.png"
          Subject = "(Hero Healing)"
          Score = winningScore }
        
type OmnipotentAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> List.concat [d.ObserversPurchased; d.SentriesPurchased]) threshold playerStats
            
        { Name = "Omnipotent"
          Type = AwardType.DotaOmnipotent
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/ufXQ6aH.png"
          Subject = "(Wards Purchased)"
          Score = winningScore }
        
type FeederAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Deaths) threshold playerStats
            
        { Name = "Feeder"
          Type = AwardType.DotaFeeder
          Color = DiscordColor.Purple
          IconUrl = "https://i.imgur.com/WLS7Av9.png"
          Subject = "(Deaths)"
          Score = winningScore }
    
    