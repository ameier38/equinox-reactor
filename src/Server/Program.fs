open Fable.SignalR
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Serilog
open Server.Config

let configureServices (config:Config) (services:IServiceCollection) =
    services
        .AddSignalR(Server.Hub.settings)
        .AddSingleton<Infrastructure.Store.CosmosStore>(fun s ->
            Infrastructure.Store.CosmosStore(config.CosmosDBConfig))
        .AddSingleton<Domain.Vehicle.Service>(fun s ->
            let store = s.GetRequiredService<Infrastructure.Store.CosmosStore>()
            Domain.Vehicle.Cosmos.createService store.Context)
        .AddSingleton<Domain.Inventory.Service>(fun s ->
            let store = s.GetRequiredService<Infrastructure.Store.CosmosStore>()
            Domain.Inventory.Cosmos.createService store.Context)
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
           .MinimumLevel.Debug()
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