module Derrick.Shared

open System
open System.Runtime.Caching
open DSharpPlus.Entities

type AwardType =
    | DotaMidas = 100
    | DotaBigBrain = 101
    | DotaBruiser = 102
    | DotaSerialKiller = 103
    | DotaAccomplice = 104
    | DotaBulldozer = 105
    | DotaHumbleFarmer = 106
    | DotaEThot = 107
    | DotaOmnipotent = 108
    | DotaFeeder = 109

type UserScore =
    { Id: uint64
      Avg: float
      Max: int
      Count: int }

type Award =
    { Type: AwardType
      Name : string
      Color : DiscordColor
      IconUrl : string
      Subject : string
      Score : UserScore }

let getWinningScore propertySelector threshold (playerStats:(uint64 * 'T) list) =
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

type Games =
    | Dota = 1
    | League = 2
    
type InteractionCacheItem =
    { Interaction : DiscordInteraction
      RestrictToUser : uint64 }
    
type CacheExpiration =
    | AbsoluteExpiration of DateTimeOffset
    | SlidingExpiration of TimeSpan
    | AbsoluteAndSlidingExpiration of DateTimeOffset * TimeSpan
 
let cacheItemCallback<'T> (operation:'T -> unit) (args:CacheEntryRemovedArguments) =
    let item = args.CacheItem.Value :?> 'T
    operation item

let addCacheItem key (object:Object) expiration onRemove =
    let item = CacheItem (key, object)
    let policy = CacheItemPolicy()
    policy.RemovedCallback <- CacheEntryRemovedCallback(onRemove)
    
    match expiration with
    | AbsoluteExpiration expiration -> policy.AbsoluteExpiration <- expiration
    | SlidingExpiration expiration -> policy.SlidingExpiration <- expiration
    | AbsoluteAndSlidingExpiration (abs, sliding) ->
        policy.AbsoluteExpiration <- abs
        policy.SlidingExpiration <- sliding
        
    MemoryCache.Default.Add(item, policy)

let envVars =
    Environment.GetEnvironmentVariables()
    |> Seq.cast<System.Collections.DictionaryEntry>
    |> Seq.map (fun d -> d.Key :?> string, d.Value :?> string)
    |> dict

let getEnvValueOrThrow envKey =
    let succ, value = envVars.TryGetValue envKey
    if not succ then
        raise (ApplicationException $"Missing environment variable '%s{envKey}'")
    value