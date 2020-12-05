module Processor.StreamStore

open Equinox.EventStore
open Shared
open System
open Serilog


type IStreamStore =
    abstract member ResolveVehicle: vehicleId:VehicleId -> Equinox.Stream<VehicleEvent,VehicleState>

type EventStoreDBStreamStore(eventstoreConfig: EventStoreDBConfig, streamConfig: StreamConfig, log: ILogger) =
    let codec =
        FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()

    let cache = Equinox.Cache("processor", 10)

    let connector =
        Connector
            (username = eventstoreConfig.User,
             password = eventstoreConfig.Password,
             reqTimeout = TimeSpan.FromSeconds(5.0),
             reqRetries = 3,
             log = Logger.SerilogNormal log)

    let eventStoreConn =
        connector.Connect("processor", Discovery.Uri(Uri(eventstoreConfig.Url)))
        |> Async.RunSynchronously

    let conn = Connection(eventStoreConn)

    let context =
        Context(conn, BatchingPolicy(maxBatchSize = 500))

    let cacheStrategy =
        CachingStrategy.SlidingWindow(cache, TimeSpan.FromMinutes(20.))

    let resolver =
        Resolver(context, codec, Aggregate.fold, Aggregate.initial, cacheStrategy)

    interface IStreamStore with

        member _.ResolveVehicle(vehicleId: VehicleId) =
            let streamName =
                FsCodec.StreamName.create streamConfig.VehicleCategory (VehicleId.toStringN vehicleId)

            let vehicleStream = resolver.Resolve(streamName)
            Equinox.Stream(log, vehicleStream, 3)
