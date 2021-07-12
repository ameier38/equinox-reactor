open Serilog
open Serilog.Events
open Server.Config

[<EntryPoint>]
let main _argv =
    let config = Config.Load()
    let logger =
        LoggerConfiguration()
            .Enrich.WithProperty("Application", config.AppName)
            .Enrich.WithProperty("Environment", config.AppEnv)
            .MinimumLevel.Is(if config.AppEnv = AppEnv.Dev then LogEventLevel.Debug else LogEventLevel.Information)
            .CreateLogger()
    Log.Logger <- logger
    Log.Debug("Debug mode")
    Log.Debug("{@Config}", config)
    try
        let store = Server.Store.LiveCosmosStore(config.CosmosDBConfig)
        let vehicleService = Server.Vehicle.Cosmos.create(store.Context)
        let inventoryService = Server.Inventory.Cosmos.create(store.Context)
        async {
            let handle = Server.Reactor.Handler.handle inventoryService
            let sink, pipeline = Server.Reactor.Handler.build store handle
            Async.Start(pipeline)
            return! sink.AwaitCompletion()
        } |> Async.RunSynchronously
    finally
        Log.CloseAndFlush()
    0 // return an integer exit code