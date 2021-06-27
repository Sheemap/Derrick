module Derrick.Services.GameApiClients.OpenDotaResponses

open Newtonsoft.Json
open Newtonsoft.Json

type PlayerMatch =
    {
      [<JsonProperty("start_time")>]
      StartTime: int
      
      [<JsonProperty("match_id")>]
      MatchId: int64
      
      [<JsonProperty("player_id")>]
      PlayerSlot: int
      
      [<JsonProperty("radiant_win")>]
      RadiantWin: bool
      
      [<JsonProperty("duration")>]
      Duration: int
      
      [<JsonProperty("game_mode")>]
      GameMode: int
      
      [<JsonProperty("lobby_type")>]
      LobbyType: int
      
      [<JsonProperty("kills")>]
      Kills: int
      
      [<JsonProperty("deaths")>]
      Deaths: int
      
      [<JsonProperty("assists")>]
      Assists: int
      
      [<JsonProperty("xp_per_min")>]
      Xpm: int
      
      [<JsonProperty("gold_per_min")>]
      Gpm: int
      
      [<JsonProperty("hero_damage")>]
      HeroDamage: int
      
      [<JsonProperty("tower_damage")>]
      TowerDamage: int
      
      [<JsonProperty("hero_healing")>]
      HeroHealing: int
      
      [<JsonProperty("last_hits")>]
      LastHits: int
      
      [<JsonProperty("purchase_ward_observer")>]
      PurchaseWardObserver: int
      
      [<JsonProperty("purchase_ward_sentry")>]
      PurchaseWardSentry: int }