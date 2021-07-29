module Reactor.Service

open Fable.SignalR
open Infrastructure.Store
open Microsoft.Extensions.Hosting
open Propulsion.CosmosStore
open Propulsion.Streams
open Serilog
open Shared.Hub
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

type Service(store:CosmosStore, inventoryService:Domain.Inventory.Service, hub:HubConnection<Action,unit,unit,Response,unit>) =
    let cts = new CancellationTokenSource()
    
    let handle (stream, span: StreamSpan<_>) =
        async {
            match stream with
            | Domain.Vehicle.Events.MatchesCategory vehicleId ->
                let events = Domain.Vehicle.Events.decode span
                let ingestionEvents =
                    events
                    |> Array.map (fun e ->
                        match e with
                        | Domain.Vehicle.Events.Added payload ->
                            Domain.Inventory.Events.VehicleAdded {| vehicleId = vehicleId; vehicle = payload.vehicle |}
                        | Domain.Vehicle.Events.Removed ->
                            Domain.Inventory.Events.VehicleRemoved {| vehicleId = vehicleId |})
                Log.Debug("ingesting events {IngestionEvents}", ingestionEvents)
                do! inventoryService.Ingest(ingestionEvents)
                return SpanResult.AllProcessed, Outcome.Completed
            | Domain.Inventory.Events.MatchesCategory _ ->
                try
                    Log.Debug("sending InventoryUpdated")
                    do! hub.Send(Action.InventoryUpdated)
                    Log.Debug("InventoryUpdated sent")
                    return SpanResult.AllProcessed, Outcome.Completed
                with ex ->
                    Log.Error(ex, "error sending InventoryUpdated")
                    return SpanResult.NoneProcessed, Outcome.Completed
            | sn ->
                return failwith $"Unknown event {sn}"
        }
        
    let run =
        async {
            do! hub.Start()
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
            