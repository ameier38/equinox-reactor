open Fable.SignalR
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Shared.Hub
open Serilog
open Serilog.Events
open Server.Config
open System.Threading
open System.Threading.Tasks

type Pinger (hub:FableHubCaller<Action,Response>) =
    let cts = new CancellationTokenSource()
    let rec loop () =
        async {
            Log.Information("Sending ping")
            do! hub.Clients.All.Send(Response.Ping "hello") |> Async.AwaitTask
            do! Async.Sleep 5000
            return! loop()
        }
        
    interface IHostedService with
        member _.StartAsync(ct) =
            let work = async { do Async.Start(loop(), cts.Token) }
            Async.StartAsTask(work, cancellationToken=ct) :> Task
        member _.StopAsync(ct) =
            let work = async { do cts.Cancel() }
            Async.StartAsTask(work, cancellationToken=ct) :> Task
    

let configureServices
        (hubSettings:SignalR.Settings<Action,Response>) =
    fun (services:IServiceCollection) ->
        services
            .AddSignalR(hubSettings)
            |> ignore
        
let configureApp (hubSettings:SignalR.Settings<Action,Response>) =
    fun (appBuilder:IApplicationBuilder) ->
        appBuilder
            // NB: rewrite route / -> /index.html 
            .UseDefaultFiles()
            // NB: service static files from wwwroot dir
            .UseStaticFiles()
            .UseSignalR(hubSettings)
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
           let store = Server.Store.LiveCosmosStore(config.CosmosDBConfig)
           let vehicleService = Server.Vehicle.Cosmos.createService store.Context
           let inventoryService = Server.Inventory.Cosmos.createService store.Context
           let reactorService = Server.Reactor.Service(store, inventoryService)
           let hubSettings = Server.Hub.createSettings vehicleService inventoryService
//           Async.Start(reactorService.StartAsync())
           WebHostBuilder()
               .UseKestrel()
               .UseSerilog()
               .ConfigureServices(configureServices hubSettings)
               .Configure(configureApp hubSettings)
               .UseUrls(config.ServerConfig.Url)
               .Build()
               .Run()
       with ex ->
           Log.Error(ex, "Error running server")
    finally
        Log.CloseAndFlush()
    0 // return an integer exit code