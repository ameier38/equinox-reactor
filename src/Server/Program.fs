open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Serilog
open Serilog.Events
open Server.Config

let configureServices (config:Config) (services:IServiceCollection) =
    services
        .AddSingleton<Server.Store.LiveCosmosStore>(fun s ->
            Server.Store.LiveCosmosStore(config.CosmosDBConfig))
        .AddSingleton<Server.Vehicle.Service>(fun s ->
            let cosmosStore = s.GetRequiredService<Server.Store.LiveCosmosStore>()
            Server.Vehicle.Cosmos.create(cosmosStore.Context))
        .AddSingleton<Server.Inventory.Service>(fun s ->
            let cosmosStore = s.GetRequiredService<Server.Store.LiveCosmosStore>()
            Server.Inventory.Cosmos.create(cosmosStore.Context))
        .AddHostedService<Server.Reactor.Service>(fun s ->
            let cosmosStore = s.GetRequiredService<Server.Store.LiveCosmosStore>()
            let inventoryService = s.GetRequiredService<Server.Inventory.Service>()
            Server.Reactor.Service(cosmosStore, inventoryService))
        |> ignore
        
[<EntryPoint>]
let main _argv =
    let config = Config.Load()
    let logger =
        LoggerConfiguration()
            .Enrich.WithProperty("Application", config.AppName)
            .Enrich.WithProperty("Environment", config.AppEnv)
            .MinimumLevel.Is(if config.AppEnv = AppEnv.Dev then LogEventLevel.Debug else LogEventLevel.Information)
            .WriteTo.Console()
            .WriteTo.Seq(config.SeqConfig.Url)
            .CreateLogger()
    Log.Logger <- logger
    Log.Debug("Debug mode")
    Log.Debug("{@Config}", config)
    try
       try
           WebHostBuilder()
               .UseKestrel()
               .UseSerilog()
               .ConfigureServices(configureServices config)
               .Configure(ignore)
               .UseUrls(config.ServerConfig.Url)
               .Build()
               .Run()
       with ex ->
           Log.Error(ex, "Error running server")
    finally
        Log.CloseAndFlush()
    0 // return an integer exit code