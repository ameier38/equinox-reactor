open Fable.Remoting.Server
open Fable.Remoting.Suave
open Processor
open Serilog
open Shared
open StreamStore
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

[<EntryPoint>]
let main _ =
    let eventStoreConfig = EventStoreDBConfig.Load()
    let streamConfig = StreamConfig.Load()
    let log = LoggerConfiguration().WriteTo.Console().CreateLogger()
    Log.Logger <- log
    let store = EventStoreDBStreamStore(eventStoreConfig, streamConfig, log)
    let api: WebPart =
        Remoting.createApi()
        |> Remoting.fromValue (Api.processorApi store)
        |> Remoting.buildWebPart
    let app: WebPart =
        choose [
            path "/heathz" >=> OK "Healthy!"
            api
        ]
    startWebServer defaultConfig app
    0 // return an integer exit code
