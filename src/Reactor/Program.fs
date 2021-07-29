open Fable.SignalR
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Serilog.Events
open Shared.Hub
open Serilog

type Hub = HubConnection<Action,unit,unit,Response,unit>

let configureServices (config:Reactor.Config) (services:IServiceCollection) =
    services
        .AddSingleton<Hub>(fun _ ->
            SignalR.Connect<Action,unit,unit,Response,unit>(fun hub ->
                hub.WithUrl(config.HubConfig.Url)
                    .WithAutomaticReconnect()))
        .AddSingleton<Infrastructure.Store.CosmosStore>(fun _ ->
            Infrastructure.Store.CosmosStore(config.CosmosDBConfig))
        .AddSingleton<Domain.Inventory.Service>(fun s ->
            let cosmosStore = s.GetRequiredService<Infrastructure.Store.CosmosStore>()
            Domain.Inventory.Cosmos.createService cosmosStore.Context)
        .AddHostedService<Reactor.Service>(fun s ->
            let hub = s.GetRequiredService<Hub>()
            let cosmosStore = s.GetRequiredService<Infrastructure.Store.CosmosStore>()
            let inventoryService = s.GetRequiredService<Domain.Inventory.Service>()
            Reactor.Service(cosmosStore, inventoryService, hub))
        |> ignore

[<EntryPoint>]
let main _argv =
    let config = Reactor.Config.Load()
    let logger =
        LoggerConfiguration()
            .Enrich.WithProperty("Application", "Reactor")
            .MinimumLevel.Is(if config.Debug then LogEventLevel.Debug else LogEventLevel.Information)
            .WriteTo.Console()
            .CreateLogger()
    Log.Logger <- logger
    Log.Debug("config {Config}", config)
    try
        try
            Host.CreateDefaultBuilder()
                .ConfigureServices(configureServices config)
                .UseSerilog()
                .Build()
                .Run()
            0
        with ex ->
            Log.Error(ex, "Error running Reactor")
            1
    finally
        Log.CloseAndFlush()
