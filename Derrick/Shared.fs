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
    
let addCacheItem key (object:Object) expiration =
    let item = CacheItem (key, object)
    let policy = CacheItemPolicy()
    
    match expiration with
    | AbsoluteExpiration expiration -> policy.AbsoluteExpiration <- expiration
    | SlidingExpiration expiration -> policy.SlidingExpiration <- expiration
    | AbsoluteAndSlidingExpiration (abs, sliding) ->
        policy.AbsoluteExpiration <- abs
        policy.SlidingExpiration <- sliding
        
    MemoryCache.Default.Add(item, policy)