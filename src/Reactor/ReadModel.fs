module Reactor.ReadModel

open FSharp.UMX
open LiteDB
open Reactor
open Shared
open Serilog
open Subscription
open System
open System.Linq
open System.Threading

type VehicleStatus =
    | Available
    | Removed

type VehicleDocument =
    { Id: ObjectId
      VehicleId: VehicleId
      Vehicle: Vehicle
      VehicleStatus: VehicleStatus
      AddedAt: DateTimeOffset
      UpdatedAt: DateTimeOffset }

type VehicleReadModel(documentStore: Store.LiteDBDocumentStore,
                      eventStoreDBConfig: EventStoreDBConfig,
                      streamConfig: StreamConfig,
                      collectionConfig: CollectionConfig) =
    let log = Log.ForContext<VehicleReadModel>()
    let stream = sprintf "$ce-%s" streamConfig.VehicleCategory |> FsCodec.StreamName.parse
    let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()

    let addVehicle (session:LiteDatabase, timestamp:DateTimeOffset, vehicleId:VehicleId, vehicle:Vehicle) =
        let vehicleIdStr = VehicleId.toStringN vehicleId
        log.Debug("Adding Vehicle-{VehicleId} {@Vehicle}", vehicleIdStr, vehicle)
        let collection = UMX.untag collectionConfig.VehiclesCollection
        let vehicles = session.GetCollection<VehicleDocument>(collection)
        let vehicleDocument = vehicles.Find(fun doc -> doc.VehicleId = vehicleId)
        if isNull (box vehicleDocument) then
            let vehicleDocument =
                { Id = ObjectId.Empty
                  VehicleId = vehicleId
                  Vehicle = vehicle
                  VehicleStatus = Available
                  AddedAt = timestamp
                  UpdatedAt = timestamp }
            vehicles.Insert(vehicleDocument) |> ignore
    
    let removeVehicle (session:LiteDatabase, timestamp:DateTimeOffset, vehicleId:VehicleId) =
        let vehicleIdStr = VehicleId.toStringN vehicleId
        log.Debug("Removing Vehicle-{VehicleId}", vehicleIdStr)
        let collection = UMX.untag collectionConfig.VehiclesCollection
        let vehicles = session.GetCollection<VehicleDocument>(collection)
        let vehicleDocument = vehicles.Find(fun doc -> doc.VehicleId = vehicleId).FirstOrDefault()
        if isNull (box vehicleDocument) then
            log.Error("Vehicle-{VehicleId} not found", vehicleIdStr)
            failwithf "Vehicle-%s not found" vehicleIdStr
        else
            let vehicleDocument =
                { vehicleDocument with
                    VehicleStatus = Removed
                    UpdatedAt = timestamp }
            vehicles.Update(vehicleDocument) |> ignore

    let handleEvent: Subscription.EventHandler =
        fun (event:FsCodec.ITimelineEvent<byte[]>) ->
            async {
                let work (session:LiteDatabase) =
                    let timestamp = event.Timestamp
                    match codec.TryDecode(event) with
                    | Some vehicleEvent ->
                        match vehicleEvent with
                        | VehicleAdded payload ->
                            addVehicle(session, timestamp, payload.VehicleId, payload.Vehicle)
                        | VehicleEvent.VehicleRemoved payload ->
                            removeVehicle(session, timestamp, payload.VehicleId)
                    | None -> ()
                    documentStore.UpdateCheckpoint(session, collectionConfig.VehiclesCollection, event.Index)
                documentStore.Transact(work)
            }

    let subscription = EventStoreDBSubscription(eventStoreDBConfig, collectionConfig.VehiclesCollection, stream, handleEvent)

    member _.StartAsync(cancellationToken:CancellationToken) =
        async {
            let checkpoint = 
                documentStore.GetCheckpoint(collectionConfig.VehiclesCollection)
                |> Option.map Checkpoint.StreamPosition
                |> Option.defaultValue Checkpoint.StreamStart
            do! subscription.SubscribeAsync(checkpoint, cancellationToken)
        }
