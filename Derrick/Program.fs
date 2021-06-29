//// Learn more about F# at http://fsharp.org
//// See the 'F# Tutorial' project for more help.
open Derrick

[<EntryPoint>]
let main argv =
    BotCore.discord.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
    ScheduleCore.loop
    0 // return an integer exit code