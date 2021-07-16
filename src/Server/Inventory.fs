module Server.Inventory

open Equinox.CosmosStore
open FsCodec.NewtonsoftJson
open Serilog
open Shared.Types

let [<Literal>] private Category = "Inventory"
let streamName () = FsCodec.StreamName.create Category "0"

type Command =
    | Ingest of version:int64 * added:InventoriedVehicle[] * removed:VehicleId[]

type Event =
    | Updated of {| version: int64; vehicles: InventoriedVehicle[] |}
    interface TypeShape.UnionContract.IUnionContract

module Event =
    let codec = Codec.Create<Event>()
    let decode (span:Propulsion.Streams.StreamSpan<_>) =
        span.events
        |> Array.choose codec.TryDecode
    let (|MatchesCategory|_|) (stream:FsCodec.StreamName) =
        match stream with
        | FsCodec.StreamName.CategoryAndId (Category, _) -> Some ()
        | _ -> None

type State = { version: int64; vehicles: InventoriedVehicle[] }

module Fold =

    let initial = { version = 0L; vehicles = Array.empty }

    let evolve (state:State) (event:Event): State =
        match event with
        | Updated payload ->
            { state with
                version = payload.version
                vehicles = payload.vehicles }
            
    let fold: State -> seq<Event> -> State = Seq.fold evolve

let interpret (command:Command) (state:State) =
    match command with
    | Ingest (version, added, removed) ->
        Log.Information("Added: {Added}; Removed: {Removed}", added, removed)
        let vehicles =
            state.vehicles
            |> Array.append added
            |> Array.filter (fun v -> not (removed |> Array.contains v.vehicleId))
        [Updated {| version = version; vehicles = vehicles |}]
    
type Service (resolve:unit -> Equinox.Decider<Event,State>) =
    
    let transact command =
        let decider = resolve ()
        decider.Transact(interpret command)
    
    member _.Update(version:int64, added:InventoriedVehicle[], removed:VehicleId[]) =
        transact (Ingest (version, added, removed))
        
    member _.Read(): Async<Inventory> =
        let decider = resolve ()
        decider.Query(fun s -> { vehicles = s.vehicles; count = s.vehicles.Length })
        
module Cosmos =
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let createService context =
        let category = CosmosStoreCategory(context, Event.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
        Service(streamName >> category.Resolve >> Equinox.createDecider)
