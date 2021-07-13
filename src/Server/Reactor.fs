module Server.Reactor

open Propulsion.CosmosStore
open Propulsion.Streams
open Serilog
open System
open Types

// Each outcome from `handle` is passed to `HandleOk` or `HandleExn` by the scheduler, DumpStats is called at `statsInterval`
// The incoming calls are all sequential - the logic does not need to consider concurrent incoming calls
type Stats(log, statsInterval, stateInterval) =
    inherit Propulsion.Streams.Stats<int>(log, statsInterval, stateInterval)

    let mutable totalCount = 0

    // TODO consider best balance between logging or gathering summary information per handler invocation
    // here we don't log per invocation (such high level stats are already gathered and emitted) but accumulate for periodic emission
    override _.HandleOk count =
        totalCount <- totalCount + count
    // TODO consider whether to log cause of every individual failure in full (Failure counts are emitted periodically)
    override _.HandleExn(log, exn) =
        log.Information(exn, "Unhandled")

    override _.DumpStats() =
        log.Information(" Total events processed {total}", totalCount)
        totalCount <- 0

            
module Handler =
            
//    let handle (_stream, span: Propulsion.Streams.StreamSpan<_>) = async {
//        let r = System.Random()
//        let ms = r.Next(1, span.events.Length)
//        do! Async.Sleep ms
//        return Propulsion.Streams.SpanResult.AllProcessed, span.events.Length }
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
                    return SpanResult.AllProcessed, span.events.Length
                | _ ->
                    return SpanResult.AllProcessed, span.events.Length
            }
        
let mapToStreamItems docs : StreamEvent<_> seq =
    docs
    |> Seq.collect EquinoxNewtonsoftParser.enumStreamEvents
        
let build
        (store:Store.LiveCosmosStore)
        (handle:FsCodec.StreamName * StreamSpan<_> -> Async<SpanResult * int>) =
    let maxReadAhead, maxConcurrentStreams = 2, 8
    let stats = Stats(Log.Logger, TimeSpan.FromMinutes 1., TimeSpan.FromMinutes 2.)
    let sink = StreamsProjector.Start(Log.Logger, maxReadAhead, maxConcurrentStreams, handle, stats, TimeSpan.FromMinutes 2.)
    let pipeline =
        use observer = CosmosStoreSource.CreateObserver(Log.Logger, sink.StartIngester, mapToStreamItems)
        CosmosStoreSource.Run(Log.Logger, store.StoreContainer, store.LeaseContainer, "Reactor", observer, startFromTail=false)
    sink, pipeline
