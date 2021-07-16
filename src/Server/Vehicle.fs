module Server.Vehicle

open Equinox.CosmosStore
open Shared.Types
open FsCodec.NewtonsoftJson

let [<Literal>] private Category = "Vehicle"
let streamName id = FsCodec.StreamName.create Category (VehicleId.toString id)

type Command =
    | Add of Vehicle
    | Remove

type Event =
    | Added of {| vehicle: Vehicle |}
    | Removed
    interface TypeShape.UnionContract.IUnionContract
    
module Event =
    let codec = Codec.Create<Event>()
    let decode (span:Propulsion.Streams.StreamSpan<_>) =
        span.events
        |> Array.choose codec.TryDecode
    let (|MatchesCategory|_|) (stream:FsCodec.StreamName) =
        match stream with
        | FsCodec.StreamName.CategoryAndId (Category, vehicleId) -> Some (VehicleId.parse vehicleId)
        | _ -> None

type State =
    | DoesNotExist
    | Exists
    
module Fold =
    
    let initial = DoesNotExist

    let evolve (_state:State) (event:Event): State =
        match event with
        | Added _ -> Exists
        | Removed -> DoesNotExist

    let fold: State -> seq<Event> -> State = Seq.fold evolve
    
let interpret (command:Command) (state:State) =
    match state, command with
    | DoesNotExist, Add vehicle ->
        [Added {| vehicle = vehicle |}]
    | Exists, Remove ->
        [Removed]
    | Exists, Add _ ->
        failwith "Vehicle already exists"
    | DoesNotExist, Remove ->
        failwith "Vehicle does not exist"
            
type Service (resolve:VehicleId -> Equinox.Decider<Event,State>) =
    
    let transact vehicleId command =
        let decider = resolve vehicleId
        decider.Transact(interpret command)
    
    member _.Add(vehicleId, vehicle) =
        transact vehicleId (Add vehicle)
        
    member _.Remove(vehicleId) =
        transact vehicleId Remove
        
module Cosmos =
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let createService context =
        let category = CosmosStoreCategory(context, Event.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
        Service(streamName >> category.Resolve >> Equinox.createDecider)
