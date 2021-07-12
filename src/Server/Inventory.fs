module Server.Inventory

open Equinox.CosmosStore
open Shared.Types
open Serilog

let [<Literal>] private Category = "Inventory"

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
    
type Service (store:Store.LiveCosmosStore) =
    let log = Log.ForContext<Service>()
    let streamName = FsCodec.StreamName.create Category "0"
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let category = CosmosStoreCategory(store.Context, Event.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
    
    member _.Update(added:InventoriedVehicle[], removed:VehicleId[]) =
        let stream = category.Resolve(streamName)
        let decider = Equinox.Decider(log, stream, maxAttempts=3)
        let command = Update (added, removed)
        decider.Transact(interpret command)
        
    member _.Read() =
        async {
            let vehicle1: InventoriedVehicle =
                { vehicleId = VehicleId.create()
                  vehicle = { make = "Tesla"; model = "S"; year = 2020 } }
            let vehicle2: InventoriedVehicle =
                { vehicleId = VehicleId.create()
                  vehicle = { make = "Toyota"; model = "Tacoma"; year = 2017 } }
            let vehicles = [| vehicle1; vehicle2 |]
            return { vehicles = vehicles ; count = vehicles.Length }
        }
//        let stream = category.Resolve(streamName)
//        let decider = Equinox.Decider(log, stream, maxAttempts=3)
//        decider.Query(id)
        