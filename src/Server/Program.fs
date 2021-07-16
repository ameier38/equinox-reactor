open Fable.SignalR
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Shared.Hub
open Serilog
open Serilog.Events
open Server.Config

let configureServices (config:Config) (services:IServiceCollection) =
    services
        .AddSignalR(Server.Hub.settings)
        .AddSingleton<Server.Store.LiveCosmosStore>(fun s ->
            Server.Store.LiveCosmosStore(config.CosmosDBConfig))
        .AddSingleton<Server.Vehicle.Service>(fun s ->
            let store = s.GetRequiredService<Server.Store.LiveCosmosStore>()
            Server.Vehicle.Cosmos.createService store.Context)
        .AddSingleton<Server.Inventory.Service>(fun s ->
            let store = s.GetRequiredService<Server.Store.LiveCosmosStore>()
            Server.Inventory.Cosmos.createService store.Context)
        .AddHostedService<Server.Reactor.Service>(fun s ->
            let store = s.GetRequiredService<Server.Store.LiveCosmosStore>()
            let inventoryService = s.GetRequiredService<Server.Inventory.Service>()
            let hub = s.GetRequiredService<FableHubCaller<Action,Response>>()
            Server.Reactor.Service(store, inventoryService, hub))
        |> ignore
        
let configureApp (appBuilder:IApplicationBuilder) =
        appBuilder
            // NB: rewrite route / -> /index.html 
            .UseDefaultFiles()
            // NB: service static files from wwwroot dir
            .UseStaticFiles()
            .UseSignalR(Server.Hub.settings)
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
               .UseSerilog()
               .UseKestrel()
               .ConfigureServices(configureServices config)
               .Configure(configureApp)
               .UseUrls(config.ServerConfig.Url)
               .Build()
               .Run()
       with ex ->
           Log.Error(ex, "Error running server")
    finally
        Log.CloseAndFlush()
    0 // return an integer exit code