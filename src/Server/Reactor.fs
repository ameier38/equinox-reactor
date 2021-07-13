module Server.Reactor

open Propulsion.CosmosStore
open Propulsion.Streams
open Serilog
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

type Service(store:Store.LiveCosmosStore, inventoryService:Inventory.Service) =
    let handle (stream, span: StreamSpan<_>) =
        async {
            match stream with
            | Vehicle.Event.MatchesCategory vehicleId ->
                let events = Vehicle.Event.decode span
                let initial: InventoriedVehicle[] * VehicleId[] = Array.empty, Array.empty
                let added, removed =
                    (initial, events)
                    ||> Array.fold (fun (added, removed) e ->
                        match e with
                        | Vehicle.VehicleAdded payload ->
                            let inventoriedVehicle = { vehicleId = vehicleId; vehicle = payload.vehicle }
                            added |> Array.append [| inventoriedVehicle |], removed
                        | Vehicle.VehicleRemoved _ -> added, removed |> Array.filter (fun vid -> vid <> vehicleId))
                do! inventoryService.Update(span.Version, added, removed)
                return SpanResult.AllProcessed, Outcome.Completed
            | _ ->
                return SpanResult.AllProcessed, Outcome.Skipped
        }
        
    member _.StartAsync() =
        async {
            try
                let stats = Stats(Log.Logger, System.TimeSpan.FromMinutes 1., System.TimeSpan.FromMinutes 5.)
                let sink = StreamsProjector.Start(Log.Logger, 10, 1, handle, stats, System.TimeSpan.FromMinutes 1.)
                let mapContent docs =
                     docs
                     |> Seq.collect EquinoxNewtonsoftParser.enumStreamEvents
                use observer = CosmosStoreSource.CreateObserver(Log.Logger, sink.StartIngester, mapContent)
                let pipeline = CosmosStoreSource.Run(Log.Logger, store.StoreContainer, store.LeaseContainer, "Reactor", observer, startFromTail=false)
                Async.Start(pipeline)
                do! sink.AwaitCompletion()
            with ex ->
                Log.Error(ex, "Error running reactor")
                raise ex
        }
        