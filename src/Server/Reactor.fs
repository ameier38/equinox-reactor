module Server.Reactor

open Microsoft.Extensions.Hosting
open Propulsion.CosmosStore
open Propulsion.Streams
open Shared.Types
open Serilog
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
    let log = Log.ForContext<Service>()
    let cts = new CancellationTokenSource()

    let handle (stream, span: StreamSpan<_>) =
        async {
            try
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
            with ex ->
                log.Error(ex, "Error handing stream")
                return raise ex
        }

    interface IHostedService with
        member _.StartAsync(ct:CancellationToken) =
            let work =
                async {
                    try
                        let stats = Stats(log, System.TimeSpan.FromMinutes 1., System.TimeSpan.FromMinutes 5.)
                        let sink = StreamsProjector.Start(log, 10, 1, handle, stats, System.TimeSpan.FromMinutes 1.)
                        let mapContent docs =
                             docs
                             |> Seq.collect EquinoxNewtonsoftParser.enumStreamEvents
                        let observer = CosmosStoreSource.CreateObserver(log, sink.StartIngester, mapContent)
                        let pipeline = CosmosStoreSource.Run(log, store.StoreContainer, store.LeaseContainer, "Reactor", observer, startFromTail=false)
                        Async.Start(pipeline, cts.Token)
                        return! sink.AwaitCompletion()
                    with ex ->
                        log.Error(ex, "Error running Reactor")
                        return raise ex
                }
            Async.StartAsTask(work, cancellationToken=ct) :> Task
        member _.StopAsync(ct:CancellationToken) =
            let work = async { do cts.Cancel() }
            Async.StartAsTask(work, cancellationToken=ct) :> Task