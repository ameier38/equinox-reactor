module Reactor.Reactor

open Reactor
open Shared
open Serilog
open Subscription
open System.Threading

type VehicleOverviewReactor(documentStore: Store.IDocumentStore,
                            eventStoreDBConfig: EventStoreDBConfig,
                            streamConfig: StreamConfig) =
    let log = Log.ForContext<VehicleOverviewReactor>()

    let stream =
        sprintf "$ce-%s" streamConfig.VehicleCategory
        |> FsCodec.StreamName.parse

    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()

    let handleEvent: Subscription.EventHandler =
        fun (event: FsCodec.ITimelineEvent<byte []>) ->
            async {
                let timestamp = event.Timestamp
                let checkpoint = event.Index
                match codec.TryDecode(event) with
                | Some vehicleEvent ->
                    match vehicleEvent with
                    | VehicleAdded payload ->
                        do! documentStore.AddVehicle(checkpoint, timestamp, payload.VehicleId, payload.Vehicle)
                    | VehicleEvent.VehicleRemoved payload ->
                        do! documentStore.RemoveVehicle(checkpoint, timestamp, payload.VehicleId)
                | None -> log.Debug("Skipping event {EventId}", event.EventId)
            }

    let subscription = EventStoreDBSubscription(eventStoreDBConfig, "Vehicle Overview", stream, handleEvent)

    member _.StartAsync(cancellationToken: CancellationToken) =
        async {
            let! rawCheckpoint = documentStore.GetVehicleOverviewCheckpoint()

            let checkpoint =
                rawCheckpoint
                |> Option.map Checkpoint.StreamPosition
                |> Option.defaultValue Checkpoint.StreamStart

            do! subscription.SubscribeAsync(checkpoint, cancellationToken)
        }

type VehicleCountReactor(cache: Cache.ICache, eventStoreDBConfig: EventStoreDBConfig, streamConfig: StreamConfig) =
    let log = Log.ForContext<VehicleCountReactor>()

    let stream =
        sprintf "$ce-%s" streamConfig.VehicleCategory
        |> FsCodec.StreamName.parse

    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()

    let handleEvent: Subscription.EventHandler =
        fun (event: FsCodec.ITimelineEvent<byte []>) ->
            async {
                let checkpoint = event.Index
                match codec.TryDecode(event) with
                | Some vehicleEvent ->
                    log.Debug("Handling event {EventId}", event.EventId)
                    match vehicleEvent with
                    | VehicleAdded _ -> do! cache.IncrementVehicleCount(checkpoint)
                    | VehicleEvent.VehicleRemoved _ -> do! cache.DecrementVehicleCount(checkpoint)
                | None -> log.Debug("Skipping event {EventId}", event.EventId)
            }

    let subscription = EventStoreDBSubscription(eventStoreDBConfig, "Vehicle Count", stream, handleEvent)

    member _.StartAsync(cancellationToken: CancellationToken) =
        async {
            let! rawCheckpoint = cache.GetVehicleCountCheckpoint()

            let checkpoint =
                match rawCheckpoint with
                | 0L -> Checkpoint.StreamStart
                | other -> Checkpoint.StreamPosition other

            do! subscription.SubscribeAsync(checkpoint, cancellationToken)
        }
