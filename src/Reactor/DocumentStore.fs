module Reactor.Store

open EventStore.ClientAPI
open FSharp.UMX
open LiteDB
open LiteDB.FSharp
open Serilog
open Shared
open System
open System.Linq

[<CLIMutable>]
type CheckpointDocument =
    { Id: ObjectId
      Collection: string
      Checkpoint: int64 }

type LiteDBDocumentStore(liteDBConfig: LiteDBConfig, collectionConfig: CollectionConfig) =
    let log = Log.ForContext<LiteDBDocumentStore>()
    let mapper = FSharpBsonMapper()

    let withDb (f: LiteDatabase -> 'T) =
        use db =
            new LiteDatabase(liteDBConfig.DataPath, mapper)

        f db

    member _.GetCheckpoint(collection: string) =
        log.Debug("Getting checkpoint for {Collection}", collection)
        withDb (fun db ->
            let checkpoints =
                db.GetCollection<CheckpointDocument>(collectionConfig.CheckpointsCollection)

            let collection = UMX.untag collection

            let checkpointDocument =
                checkpoints.Find(fun doc -> doc.Collection = collection).FirstOrDefault()

            if isNull (box checkpointDocument) then None else Some checkpointDocument.Checkpoint)

    member _.UpdateCheckpoint(session: LiteDatabase, collection: string, checkpoint: int64) =
        log.Debug("Updating {Collection} checkpoint to {Checkpoint}", collection, checkpoint)

        let checkpoints =
            session.GetCollection<CheckpointDocument>(collectionConfig.CheckpointsCollection)

        let collection = UMX.untag collection

        let checkpointDocument =
            checkpoints.Find(fun doc -> doc.Collection = collection).FirstOrDefault()

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

    member _.Transact(work: LiteDatabase -> unit) =
        withDb (fun db ->
            log.Debug("Starting transaction")
            if db.BeginTrans() then
                try
                    work db
                    log.Debug("Commiting transaction")
                    if db.Commit() then
                        log.Debug("Succesfully commited transaction")
                    else
                        failwith "Failed to commit transaction"
                with ex ->
                    log.Error(ex, "Could not complete transaction")
                    if db.Rollback() then
                        log.Debug("Successfully rolled back transaction")
                    else
                        let msg = "Failed to rollback transaction"
                        log.Error(msg)
                        failwith msg
            else
                let msg = "Could not start transaction"
                log.Error(msg)
                failwith msg
        )
