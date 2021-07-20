module Domain.Vehicle

open Shared.Types
open FsCodec.NewtonsoftJson

let [<Literal>] private Category = "Vehicle"
let streamName id = FsCodec.StreamName.create Category (VehicleId.toString id)

[<RequireQualifiedAccess>]
module Events =
    type Vehicle = { make: string; model: string; year: int }
    
    type Event =
        | Added of {| vehicle: Vehicle |}
        | Removed
        interface TypeShape.UnionContract.IUnionContract
    
    let codec = Codec.Create<Event>()
    let decode (span:Propulsion.Streams.StreamSpan<_>) =
        span.events
        |> Array.choose (fun e ->
            match codec.TryDecode e with
            | Some decoded -> Some (e.Index, decoded)
            | None -> None)
    let (|MatchesCategory|_|) (stream:FsCodec.StreamName) =
        match stream with
        | FsCodec.StreamName.CategoryAndId (Category, vehicleId) -> Some (VehicleId.parse vehicleId)
        | _ -> None

type State =
    | Initial
    | Running
    | Removed
    
module Fold =
    
    let initial = Initial

    let evolve (_state:State) = function
        | Events.Added _ -> Running
        | Events.Removed -> Removed

    let fold: State -> seq<Events.Event> -> State = Seq.fold evolve
    
let interpretAdd (vehicle:Events.Vehicle) = function
    | Initial -> [Events.Added {| vehicle = vehicle |}]
    | Running -> [] // Resubmission assumed to have identical properties
    | Removed -> []
    
let interpretRemove = function
    | Initial -> failwith "Vehicle does not exist"
    | Running -> [Events.Removed]
    | Removed -> []
            
type Service (resolve:VehicleId -> Equinox.Decider<Events.Event,State>) =
    
    member _.Add(vehicleId, vehicle) =
        let decider = resolve vehicleId
        decider.Transact(interpretAdd vehicle)
        
    member _.Remove(vehicleId) =
        let decider = resolve vehicleId
        decider.Transact(interpretRemove)
        
module Cosmos =
    open Equinox.CosmosStore
    
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let createService context =
        let category = CosmosStoreCategory(context, Events.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
        Service(streamName >> category.Resolve >> Equinox.createDecider)
