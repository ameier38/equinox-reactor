module Server.Reactor

open Fable.SignalR
open Microsoft.Extensions.Hosting
open Propulsion.CosmosStore
open Propulsion.Streams
open Serilog
open Shared.Hub
open Shared.Types
open System.Threading
open System.Threading.Tasks

[<RequireQualifiedAccess>]
type Outcome = Completed | Skipped

/// Gathers stats based on the outcome of each Span processed, periodically including them in the Sink summaries
type Stats(log, statsInterval, stateInterval) =
    inherit Propulsion.Streams.Stats<Outcome>(log, statsInterval, stateInterval)

    let mutable completed, skipped = 0, 0
    member val StatsInterval = statsInterval

    override _.HandleOk res = res |> function
        | Outcome.Completed -> completed <- completed + 1
        | Outcome.Skipped -> skipped <- skipped + 1
    override _.HandleExn(log, exn) =
        log.Information(exn, "Unhandled")

    override _.DumpStats() =
        if completed <> 0 || skipped <> 0 then
            log.Information(" Completed {completed} Skipped {skipped}", completed, skipped)
            completed <- 0; skipped <- 0

type Service(store:Store.LiveCosmosStore, inventoryService:Inventory.Service, hub:FableHubCaller<Action,Response>) =
    let cts = new CancellationTokenSource()
    
    let handle (stream, span: StreamSpan<_>) =
        async {
            match stream with
            | Vehicle.Event.MatchesCategory vehicleId ->
                let events = Vehicle.Event.decode span
                let initial = Array.empty, Array.empty
                let added, removed =
                    (initial, events)
                    ||> Array.fold (fun (added, removed) e ->
                        match e with
                        | Vehicle.Event.Added payload ->
                            let inventoriedVehicle = { vehicleId = vehicleId; vehicle = payload.vehicle }
                            added |> Array.append [| inventoriedVehicle |], removed
                        | Vehicle.Event.Removed ->
                            added, removed |> Array.append [| vehicleId |])
                Log.Information("Updating inventory")
                do! inventoryService.Update(span.Version, added, removed)
                return SpanResult.AllProcessed, Outcome.Completed
            | Inventory.Event.MatchesCategory _ ->
                let event = Inventory.Event.decode span |> Array.last
                match event with
                | Inventory.Event.Updated payload ->
                    let inventory = { vehicles = payload.vehicles; count = payload.vehicles.Length }
                    Log.Information("Inventory updated; notifying clients")
                    do! hub.Clients.All.Send(Response.InventoryUpdated(inventory)) |> Async.AwaitTask
                return SpanResult.AllProcessed, Outcome.Completed
            | _ ->
                return SpanResult.AllProcessed, Outcome.Skipped
        }
        
    let run =
        async {
            let stats = Stats(Log.Logger, System.TimeSpan.FromMinutes 1., System.TimeSpan.FromMinutes 5.)
            let sink = StreamsProjector.Start(Log.Logger, 10, 1, handle, stats, System.TimeSpan.FromMinutes 1.)
            let mapContent = Seq.collect EquinoxNewtonsoftParser.enumStreamEvents
            use observer = CosmosStoreSource.CreateObserver(Log.Logger, sink.StartIngester, mapContent)
            let pipeline = CosmosStoreSource.Run(Log.Logger, store.StoreContainer, store.LeaseContainer, "Reactor", observer, startFromTail=false)
            Async.Start(pipeline, cancellationToken=cts.Token)
            do! sink.AwaitCompletion()
        }
        
    interface IHostedService with
        
        member _.StartAsync(ct:CancellationToken) =
            let work = async { do Async.Start(run, cts.Token) }
            Async.StartAsTask(work, cancellationToken=ct) :> Task
        
        member _.StopAsync(ct:CancellationToken) =
            let work = async { do cts.Cancel() }
            Async.StartAsTask(work, cancellationToken=ct) :> Task
        