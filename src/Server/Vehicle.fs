module Server.Vehicle

open Equinox.CosmosStore
open Shared.Types
open FsCodec.NewtonsoftJson

let [<Literal>] private Category = "Vehicle"
let streamName id = FsCodec.StreamName.create Category (VehicleId.toString id)

type Command =
    | AddVehicle of Vehicle
    | RemoveVehicle

type Event =
    | VehicleAdded of {| vehicle: Vehicle |}
    | VehicleRemoved
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

    let evolve (_:State) (event:Event): State =
        match event with
        | VehicleAdded _ -> Exists
        | VehicleRemoved _  -> DoesNotExist

    let fold: State -> seq<Event> -> State = Seq.fold evolve
    
let interpret (command:Command) (state:State) =
    match state, command with
    | DoesNotExist, AddVehicle vehicle ->
        let vehicleAdded = VehicleAdded {| vehicle = vehicle |}
        [vehicleAdded]
    | Exists, AddVehicle _ ->
        failwith "Vehicle already added"
    | DoesNotExist, RemoveVehicle ->
        failwith "Vehicle not found"
    | Exists, RemoveVehicle ->
        let vehicleRemoved = VehicleRemoved
        [vehicleRemoved]
            
type Service (resolve:VehicleId -> Equinox.Decider<Event,State>) =
    
    let transact vehicleId command =
        let decider = resolve vehicleId
        decider.Transact(interpret command)
    
    member _.AddVehicle(vehicleId, vehicle) =
        transact vehicleId (AddVehicle vehicle)
        
    member _.RemoveVehicle(vehicleId) =
        transact vehicleId RemoveVehicle
        
module Cosmos =
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let createService context =
        let category = CosmosStoreCategory(context, Event.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
        Service(streamName >> category.Resolve >> Equinox.createDecider)
