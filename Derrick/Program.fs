//// Learn more about F# at http://fsharp.org
//// See the 'F# Tutorial' project for more help.
open System.Threading
open Derrick
open Microsoft.Extensions.Hosting
open Serilog
open Serilog.Sinks.SystemConsole
open Serilog.Sinks.File

let configureLogging () =
    Log.Logger <-
        LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("log-.txt", rollingInterval = RollingInterval.Day)
            .MinimumLevel.Information()
            .CreateLogger()
            
[<EntryPoint>]
let main argv =
    configureLogging()
    BotCore.discord.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
    ScheduleCore.loop()
    0 // return an integer exit code