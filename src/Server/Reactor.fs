module Server.Reactor

open Propulsion.CosmosStore
open Propulsion.Streams
open Serilog
open System
open Types

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
            
module Handler =
            
    let mapToStreamItems docs : StreamEvent<_> seq =
        docs
        |> Seq.collect EquinoxNewtonsoftParser.enumStreamEvents
        
    let handle
        (inventoryService:Inventory.Service) =
        fun (stream, span: StreamSpan<_>) ->
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
                    do! inventoryService.Update(added, removed)
                    return SpanResult.AllProcessed, Outcome.Completed
                | _ ->
                    return SpanResult.AllProcessed, Outcome.Skipped
            }
        
    let build
            (store:Store.LiveCosmosStore)
            (handle:FsCodec.StreamName * StreamSpan<_> -> Async<SpanResult * Outcome>) =
        let maxReadAhead, maxConcurrentStreams = 2, 8
        let stats = Stats(Log.Logger, TimeSpan.FromMinutes 1., TimeSpan.FromMinutes 2.)
        let sink = StreamsProjector.Start(Log.Logger, maxReadAhead, maxConcurrentStreams, handle, stats, TimeSpan.FromMinutes 2.)
        let pipeline =
            use observer = CosmosStoreSource.CreateObserver(Log.Logger, sink.StartIngester, mapToStreamItems)
            CosmosStoreSource.Run(Log.Logger, store.StoreContainer, store.LeaseContainer, "Reactor", observer, startFromTail=false)
        sink, pipeline
