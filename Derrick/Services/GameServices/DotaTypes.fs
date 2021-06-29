module Derrick.Services.GameServices.DotaTypes

open DSharpPlus.Entities
open Derrick.Shared

type UserData =
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
    
type UserScore =
    { Id: uint64
      Avg: float
      Max: int
      Count: int }
    
let Adjectives =
    [ "an awesome"
      "a fantastic"
      "a delicious"
      "a delightful"
      "a scrumptious"
      "a boolin"
      "a sick"
      "a rad"
      "a tubular"
      "a cool"
      "a nice"
      "a super"
      "a neato"
      "an okayish"
      "an alright"
      "a fine"
      "a decent"
      "a mediocre"
      "a chill"
      "an amazing" ]
  
let Exclamations =
    [ "Wow!"
      "***WOW!***"
      "Waow."
      "Nice."
      "Neat."
      "Neato."
      "Cool."
      "Cool!"
      "***RAD!***"
      "Dope."
      "Dope!"
      "Slick."
      "Chill."
      "Not bad."
      "Eh."
      "Super." ]
    
let msgGen score =
    let adj = Adjectives.[System.Random().Next(0, (List.length Adjectives) - 1)]
    let excl = Exclamations.[System.Random().Next(0, (List.length Exclamations) - 1)]
    $"<@%i{score.Id}> had %s{adj} **%.2f{score.Avg}** average with a max of **%i{score.Max}** over **%i{score.Count}** games! %s{excl}"

let getWinningScore propertySelector threshold (playerStats:(uint64 * UserData) list) =
    playerStats
    |> List.map (fun (userId, data) ->
        (userId, propertySelector data))
    |> List.map (fun (userId, scores) ->
        { Id = userId
          Avg = (List.average (List.map float scores))
          Max = (List.max scores)
          Count = (List.length scores) })
    |> List.filter (fun score -> score.Count >= threshold)
    |> List.maxBy (fun score -> score.Avg)

type MidasAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Gpm) threshold playerStats
            
        { Name = "Midas"
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(GPM)"
          Message = msgGen winningScore }
        
type BigBrainAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Xpm) threshold playerStats
            
        { Name = "Big Brain"
          Color = DiscordColor.Aquamarine
          IconUrl = "https://i.imgur.com/79NQbnw.png"
          Subject = "(XPM)"
          Message = msgGen winningScore }
        
type BruiserAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.HeroDamage) threshold playerStats
            
        { Name = "Bruiser"
          Color = DiscordColor.Azure
          IconUrl = "https://i.imgur.com/4fSheAx.png"
          Subject = "(Hero Damage)"
          Message = msgGen winningScore }
        
type SerialKillerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Kills) threshold playerStats
            
        { Name = "Serial Killer"
          Color = DiscordColor.DarkRed
          IconUrl = "https://i.imgur.com/6ZH6NxK.png"
          Subject = "(Kills)"
          Message = msgGen winningScore }
        
type AccompliceAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Assists) threshold playerStats
            
        { Name = "Accomplice"
          Color = DiscordColor.White
          IconUrl = "https://i.imgur.com/bre8cOp.png"
          Subject = "(Assists)"
          Message = msgGen winningScore }
        
type BulldozerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.TowerDamage) threshold playerStats
            
        { Name = "Bulldozer"
          Color = DiscordColor.Red
          IconUrl = "https://i.imgur.com/rGg6IGA.png"
          Subject = "(Structure Damage)"
          Message = msgGen winningScore }
        
type HumbleFarmerAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.LastHits) threshold playerStats
            
        { Name = "Humble Farmer"
          Color = DiscordColor.Goldenrod
          IconUrl = "https://i.imgur.com/Xb2DnfN.png"
          Subject = "(Last Hits)"
          Message = msgGen winningScore }
        
type EThotAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.HeroHealing) threshold playerStats
            
        { Name = "E-Thot"
          Color = DiscordColor.Green
          IconUrl = "https://i.imgur.com/YPi6w8w.png"
          Subject = "(Hero Healing)"
          Message = msgGen winningScore }
        
type OmnipotentAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> List.concat [d.ObserversPurchased; d.SentriesPurchased]) threshold playerStats
            
        { Name = "Omnipotent"
          Color = DiscordColor.Gold
          IconUrl = "https://i.imgur.com/ufXQ6aH.png"
          Subject = "(Wards Purchased)"
          Message = msgGen winningScore }
        
type FeederAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Deaths) threshold playerStats
            
        { Name = "Feeder"
          Color = DiscordColor.Purple
          IconUrl = "https://i.imgur.com/WLS7Av9.png"
          Subject = "(Deaths)"
          Message = msgGen winningScore }
    
    