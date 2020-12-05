module Reactor.Store

open LiteDB
open Serilog
open Shared
open System

[<CLIMutable>]
type CheckpointDocument =
    { Id: ObjectId
      Collection: string
      Checkpoint: int64 }

type IDocumentStore =
    abstract GetVehicleOverviewCheckpoint: unit -> Async<int64 option>
    abstract AddVehicle: checkpoint:int64 * timestamp:DateTimeOffset * vehicleId:VehicleId * vehicle:Vehicle -> Async<unit>
    abstract RemoveVehicle: checkpoint:int64 * timestamp:DateTimeOffset * vehicleId:VehicleId -> Async<unit>

type LiteDBDocumentStore(config: LiteDBConfig) =
    let log = Log.ForContext<LiteDBDocumentStore>()

    let connStr = sprintf "Filename=%s;Connection=shared" config.Filename

    let withDb (f: LiteDatabase -> 'T) =
        use db = new LiteDatabase(connStr)

        f db

    let getCheckpoint (collection: string) =
        log.Debug("Getting checkpoint for {Collection}", collection)
        withDb (fun db ->
            let checkpoints = db.GetCollection<CheckpointDocument>("checkpoints")

            let checkpointDocument = checkpoints.FindOne(fun doc -> doc.Collection = collection)

            if isNull (box checkpointDocument) then None else Some checkpointDocument.Checkpoint)

    let setCheckpoint (db: LiteDatabase, collection: string, checkpoint: int64) =
        log.Debug("Setting {Collection} checkpoint to {Checkpoint}", collection, checkpoint)

        let checkpoints = db.GetCollection<CheckpointDocument>("checkpoints")

        let checkpointDocument = checkpoints.FindOne(fun doc -> doc.Collection = collection)

        if isNull (box checkpointDocument) then
            let checkpointDocument =
                { Id = ObjectId.Empty
                  Collection = collection
                  Checkpoint = checkpoint }

            checkpoints.Insert(checkpointDocument) |> ignore
        else
            let checkpointDocument =
                { checkpointDocument with
                      Checkpoint = checkpoint }

            checkpoints.Update(checkpointDocument) |> ignore

    let transact (work: LiteDatabase -> unit) =
        withDb (fun db ->
            log.Debug("Starting transaction")
            if db.BeginTrans() then
                try
                    work db
                    if not (db.Commit()) then failwith "Failed to commit transaction"
                with ex ->
                    log.Error(ex, "Transaction failed")
                    if not (db.Rollback())
                    then failwith "Failed to rollback transaction"
            else
                failwith "Could not start transaction")

    interface IDocumentStore with
        member _.GetVehicleOverviewCheckpoint() = async { return getCheckpoint config.VehicleOverviewCollection }

        member _.AddVehicle(checkpoint: int64, timestamp: DateTimeOffset, vehicleId: VehicleId, vehicle: Vehicle) =
            let work (db: LiteDatabase) =
                let vehicleIdStr = VehicleId.toStringN vehicleId
                log.Debug("Adding Vehicle-{VehicleId} {@Vehicle}", vehicleIdStr, vehicle)

                let vehicles = db.GetCollection<ReadModel.VehicleOverview>(config.VehicleOverviewCollection)

                log.Debug("Finding Vehicle-{VehicleId}", vehicleIdStr)
                let vehicleOverview = vehicles.FindOne(fun doc -> doc.VehicleId = vehicleIdStr)

                if isNull (box vehicleOverview) then
                    let vehicleOverview: ReadModel.VehicleOverview =
                        { Id = 0
                          VehicleId = vehicleIdStr
                          Make = vehicle.Make
                          Model = vehicle.Model
                          Year = vehicle.Year
                          Status = "Available"
                          AddedAt = timestamp
                          UpdatedAt = timestamp }

                    log.Debug("Inserting {@VehicleOverview}", vehicleOverview)
                    vehicles.Insert(vehicleOverview) |> ignore
                else
                    log.Debug("Vehicle already exists: {@VehicleOverview}", vehicleOverview)
                setCheckpoint (db, config.VehicleOverviewCollection, checkpoint)

            async { return transact (work) }

        member _.RemoveVehicle(checkpoint: int64, timestamp: DateTimeOffset, vehicleId: VehicleId) =
            let work (db: LiteDatabase) =
                let vehicleIdStr = VehicleId.toStringN vehicleId
                log.Debug("Removing Vehicle-{VehicleId}", vehicleIdStr)

                let vehicles = db.GetCollection<ReadModel.VehicleOverview>(config.VehicleOverviewCollection)

                log.Debug("Finding Vehicle-{VehicleId}", vehicleIdStr)
                let vehicleOverview = vehicles.FindOne(fun doc -> doc.VehicleId = vehicleIdStr)

                if isNull (box vehicleOverview) then
                    log.Error("Vehicle-{VehicleId} not found", vehicleIdStr)
                    failwithf "Vehicle-%s not found" vehicleIdStr
                else
                    let vehicleOverview =
                        { vehicleOverview with
                              Status = "Removed"
                              UpdatedAt = timestamp }

                    log.Debug("Updating {@VehicleOverview}", vehicleOverview)
                    vehicles.Update(vehicleOverview) |> ignore
                setCheckpoint (db, config.VehicleOverviewCollection, checkpoint)

            async { return transact (work) }
