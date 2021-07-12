module Server.Inventory

open Equinox.CosmosStore
open Shared.Types
open Serilog

let [<Literal>] private Category = "Inventory"
let streamName () = FsCodec.StreamName.create Category "0"

type Command =
    | Update of added:InventoriedVehicle [] * removed:VehicleId []

type Event =
    | Updated of {| added:InventoriedVehicle []; removed:VehicleId [] |}
    interface TypeShape.UnionContract.IUnionContract

module Event =
    let codec = FsCodec.NewtonsoftJson.Codec.Create<Event>()

module Fold =

    let initial = { vehicles = Array.empty; count = 0 }

    let evolve (state:Inventory) (event:Event): Inventory =
        match event with
        | Updated payload ->
            { state with
                vehicles =
                    state.vehicles
                    |> Array.filter (fun v -> not (payload.removed |> Array.contains v.vehicleId))
                    |> Array.append payload.added
                count = state.count - payload.removed.Length + payload.added.Length }
            
    let fold: Inventory -> seq<Event> -> Inventory = Seq.fold evolve

let interpret command _state =
    match command with
    | Update (added, removed) -> [Updated {| added = added; removed = removed |}]
    
type Service (resolve:unit -> Equinox.Decider<Event,Inventory>) =
    
    member _.Update(added:InventoriedVehicle[], removed:VehicleId[]) =
        let decider = resolve ()
        let command = Update (added, removed)
        decider.Transact(interpret command)
        
    member _.Read() =
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
        decider.Query(id)
        
module Cosmos =
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let create context =
        let category = CosmosStoreCategory(context, Event.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
        Service(streamName >> category.Resolve >> Equinox.createDecider)
