module Derrick.Shared

open System
open System.Drawing
open System.Runtime.Caching
open DSharpPlus.Entities

type Award =
    { Name : string
      Color : string
      IconUrl : string
      Subject : string
      Message : string }

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