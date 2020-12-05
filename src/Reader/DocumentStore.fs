module Reader.DocumentStore

open LiteDB
open Shared
open Serilog

type IDocumentStore =
    abstract member ListVehicles: unit -> Async<ReadModel.VehicleOverview list>

type LiteDBDocumentStore(config:LiteDBConfig) =
    let log = Log.ForContext<LiteDBDocumentStore>()

    let connStr = sprintf "Filename=%s;Connection=shared" config.Filename

    let withDb (f: LiteDatabase -> 'T) =
        use db = new LiteDatabase(connStr)

        f db

    let listVehicles () =
        withDb (fun db ->
            log.Information("Listing vehicles")
            let vehicles = db.GetCollection<ReadModel.VehicleOverview>(config.VehicleOverviewCollection)
            vehicles.Find(fun v -> v.Status = "Available")
            |> Seq.toList
        )

    interface IDocumentStore with

        member _.ListVehicles() = async { return listVehicles() }
