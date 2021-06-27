module Derrick.Services.GameServices.DotaTypes

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
      LastHits: int list }
    
type UserScore =
    { Id: uint64
      Avg: float
      Max: int
      Count: int }
    
let msgGen score =
    $"<@%i{score.Id}> had an amazing **%f{score.Avg}** average with a max of **%i{score.Max}** over **%i{score.Count}** games! NEAT"

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
          Color = "16766720"
          IconUrl = "https://i.imgur.com/GMMeySI.png"
          Subject = "(GPM)"
          Message = msgGen winningScore }
        
type FeederAward(threshold) =
    member this.Award playerStats =
        let winningScore =
            getWinningScore (fun d -> d.Deaths) threshold playerStats
            
        { Name = "Feeder"
          Color = "13856728"
          IconUrl = "https://i.imgur.com/WLS7Av9.png"
          Subject = "(Deaths)"
          Message = msgGen winningScore }
    
    