module ButtonService

open System
open System.Runtime.Caching
open DSharpPlus
open DSharpPlus.Entities
open DataTypes
open Derrick.Services
open Newtonsoft.Json

type ButtonData<'T> =
    { Id: Guid
      CommandModule: string
      ParentInteraction: DiscordInteraction option
      UniqueUser: uint64 option
      Data: 'T }

type ButtonMsgData<'T> =
    { ButtonData:  ButtonData<'T>
      ButtonStyle: ButtonStyle
      ButtonTitle: string }

let saveDbRecords buttonData =
    buttonData
    |> List.map (fun d ->
        let parentInteractionId =
            match d.ParentInteraction with
            | Some i -> i.Id
            | None -> uint64 0
        let jsonData = JsonConvert.SerializeObject d.Data
        
        { Id = d.Id
          CommandModule = d.CommandModule
          ParentInteractionId = parentInteractionId
          UniqueUser = d.UniqueUser
          JsonData = jsonData
          DateCreatedUTC = DateTime.UtcNow
        })
    |> DataService.insertButtonData

let genButtonComponents buttonData =
    buttonData
    |> List.map (fun d -> d.ButtonData)
    |> saveDbRecords
    |> ignore
    
    let msgButtons =
        buttonData
        |> List.map (fun d ->
            DiscordButtonComponent(d.ButtonStyle, d.ButtonData.Id.ToString(), d.ButtonTitle))
        |> Seq.cast<DiscordComponent>
        |> Seq.toList

    let count = List.length msgButtons
    let rowAdd = min (count % 5) 1
    let rowCount = (count / 5) + rowAdd

    msgButtons
    |> List.splitInto rowCount
    |> List.map DiscordActionRowComponent
    
let getDeserializedButtonData<'T> buttonData =
    let data = JsonConvert.DeserializeObject<'T> buttonData.JsonData
    let interaction =
        match MemoryCache.Default.Get (string buttonData.ParentInteractionId) with
        | item when not (isNull item) -> Some (item :?> DiscordInteraction)
        | _ -> None
        
    { Id = buttonData.Id
      CommandModule = buttonData.CommandModule
      ParentInteraction = interaction
      UniqueUser = buttonData.UniqueUser
      Data = data }
        
    