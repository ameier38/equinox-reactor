open Reactor
open Shared
open Serilog
open System
open System.Threading

[<EntryPoint>]
let main _ =
    let debug = Env.getEnv "DEBUG" "false" |> bool.Parse
    let seqConfig = SeqConfig.Load()
    let liteDBConfig = LiteDBConfig.Load()
    let redisConfig = RedisConfig.Load()
    let eventStoreDBConfig = EventStoreDBConfig.Load()
    let streamConfig = StreamConfig.Load()
    let logger =
        LoggerConfiguration()
            .MinimumLevel.Is(if debug then Events.LogEventLevel.Debug else Events.LogEventLevel.Information)
            .Enrich.WithProperty("Application", "Reactor")
            .WriteTo.Console()
            .WriteTo.Seq(seqConfig.Url)
            .CreateLogger()
    Log.Logger <- logger
    let documentStore = Store.LiteDBDocumentStore(liteDBConfig)
    let cache = Cache.RedisCache(redisConfig)
    let vehicleOverviewReactor = Reactor.VehicleOverviewReactor(documentStore, eventStoreDBConfig, streamConfig)
    let vehicleCountReactor = Reactor.VehicleCountReactor(cache, eventStoreDBConfig, streamConfig)
    use cancellation = new CancellationTokenSource()
    Console.CancelKeyPress |> Event.add (fun _ ->
        Log.Information("shutting down...")
        cancellation.Cancel())
    Async.Parallel
        [ vehicleOverviewReactor.StartAsync(cancellation.Token)
          vehicleCountReactor.StartAsync(cancellation.Token) ]
    |> Async.Ignore
    |> Async.RunSynchronously
    Log.CloseAndFlush()
    0 // return an integer exit code
