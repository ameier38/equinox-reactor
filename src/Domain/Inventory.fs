module Domain.Inventory

open FsCodec.NewtonsoftJson
open Shared.Types

let [<Literal>] private Category = "Inventory"
// TODO: add limit and additional epoch
let streamName () = FsCodec.StreamName.create Category "0"

[<RequireQualifiedAccess>]
module Events =
    type Event =
        | VehicleAdded of {| vehicleId: VehicleId; vehicle: Vehicle.Events.Vehicle |}
        | VehicleRemoved of {| vehicleId: VehicleId |}
        interface TypeShape.UnionContract.IUnionContract

    let codec = Codec.Create<Event>()
    let decode (span:Propulsion.Streams.StreamSpan<_>) =
        span.events
        |> Array.choose codec.TryDecode
    let (|MatchesCategory|_|) = function
        | FsCodec.StreamName.CategoryAndId (Category, _) -> Some ()
        | _ -> None

module Fold =
    type State = Map<VehicleId,Vehicle.Events.Vehicle>

    let initial = Map.empty

    let evolve (state:State) = function
        | Events.VehicleAdded e ->
            state |> Map.add e.vehicleId e.vehicle
        | Events.VehicleRemoved e ->
            state |> Map.remove e.vehicleId
            
    let fold: State -> seq<Events.Event> -> State = Seq.fold evolve

let interpret (events:Events.Event[]) (state:Fold.State) =
    let exists = state.ContainsKey
    let isFresh = function
         | Events.VehicleAdded e -> (not << exists) e.vehicleId
         | Events.VehicleRemoved e -> exists e.vehicleId
    events |> Seq.filter isFresh |> Seq.toList
    
let render (state:Fold.State) =
    state
    |> Map.toSeq
    |> Seq.map (fun )
    
type Service (resolve:unit -> Equinox.Decider<Events.Event,Fold.State>) =
    
    member _.Ingest(events:Events.Event[]) =
        let decider = resolve ()
        decider.Transact(interpret events)
        
    member _.Read() =
        let decider = resolve ()
        decider.QueryEx(render)
        
module Cosmos =
    open Equinox.CosmosStore
    
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let createService context =
        let category = CosmosStoreCategory(context, Events.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
        Service(streamName >> category.Resolve >> Equinox.createDecider)
