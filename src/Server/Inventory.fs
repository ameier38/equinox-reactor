module Server.Inventory

open Equinox.CosmosStore
open FsCodec.NewtonsoftJson
open Shared.Types

let [<Literal>] private Category = "Inventory"
let streamName () = FsCodec.StreamName.create Category "0"

type Command =
    | Update of version:int64 * added:InventoriedVehicle [] * removed:VehicleId []

type Event =
    | Updated of {| vehicles:InventoriedVehicle []; count:int |}
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

type State = { version: int64; vehicles: InventoriedVehicle[]; count: int }

module Fold =

    let initial = { version = 0L; vehicles = Array.empty; count = 0 }

    let evolve (state:State) (event:Event): State =
        match event with
        | Updated payload ->
            { state with
                vehicles = payload.vehicles
                count = payload.count }
            
    let fold: State -> seq<Event> -> State = Seq.fold evolve

let interpret (command:Command) (state:State) =
    match command with
    | Update (version, added, removed) ->
        if version > state.version then
            let vehicles =
                state.vehicles
                |> Array.filter (fun v -> not (removed |> Array.contains v.vehicleId))
                |> Array.append added
            let count = state.count - removed.Length + added.Length
            [Updated {| vehicles = vehicles; count = count |}]
        else
            []
    
type Service (resolve:unit -> Equinox.Decider<Event,State>) =
    
    member _.Update(version:int64, added:InventoriedVehicle[], removed:VehicleId[]) =
        let decider = resolve ()
        let command = Update (version, added, removed)
        decider.Transact(interpret command)
        
    member _.Read(): Async<Inventory> =
//        async {
//            let vehicle1: InventoriedVehicle =
//                { vehicleId = VehicleId.create()
//                  vehicle = { make = "Tesla"; model = "S"; year = 2020 } }
//            let vehicle2: InventoriedVehicle =
//                { vehicleId = VehicleId.create()
//                  vehicle = { make = "Toyota"; model = "Tacoma"; year = 2017 } }
//            let vehicles = [| vehicle1; vehicle2 |]
//            return { vehicles = vehicles ; count = vehicles.Length }
//        }
        let decider = resolve ()
        decider.Query(fun s -> { vehicles = s.vehicles; count = s.count })
        
module Cosmos =
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let createService context =
        let category = CosmosStoreCategory(context, Event.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
        Service(streamName >> category.Resolve >> Equinox.createDecider)
