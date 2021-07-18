module Server.Inventory

open Equinox.CosmosStore
open FsCodec.NewtonsoftJson
open Shared.Types

let [<Literal>] private Category = "Inventory"
let streamName () = FsCodec.StreamName.create Category "0"

type IngestionEvent =
    | VehicleAdded of InventoriedVehicle
    | VehicleRemoved of VehicleId

type Command =
    | Ingest of events:IngestionEvent[]

type Event =
    | Ingested of {| events:IngestionEvent[] |}
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

type State = Map<VehicleId,InventoriedVehicle>

module Fold =

    let initial = Map.empty

    let evolve (state:State) (event:Event): State =
        match event with
        | Ingested payload ->
            (state, payload.events)
            ||> Array.fold (fun s e ->
                match e with
                | VehicleAdded inventoriedVehicle ->
                    match s.TryFind(inventoriedVehicle.vehicleId) with
                    | Some existingInventoriedVehicle ->
                        if inventoriedVehicle.version > existingInventoriedVehicle.version then
                            s.Add(inventoriedVehicle.vehicleId, inventoriedVehicle)
                        else
                            s
                    | None ->
                        s.Add(inventoriedVehicle.vehicleId, inventoriedVehicle)
                | VehicleRemoved vehicleId ->
                    s.Remove(vehicleId))
            
    let fold: State -> seq<Event> -> State = Seq.fold evolve

let interpret (command:Command) (_state:State) =
    match command with
    | Ingest events ->
        [Ingested {| events = events |}]
    
type Service (resolve:unit -> Equinox.Decider<Event,State>) =
    
    let transact command =
        let decider = resolve ()
        decider.Transact(interpret command)
    
    member _.Ingest(events:IngestionEvent[]) =
        transact (Ingest events)
        
    member _.Read(): Async<Inventory> =
        let decider = resolve ()
        decider.Query(fun s ->
            let vehicles = s |> Map.toArray |> Array.map snd
            { vehicles = vehicles; count = vehicles.Length })
        
module Cosmos =
    let cacheStrategy = CachingStrategy.NoCaching
    let accessStrategy = AccessStrategy.Unoptimized
    let createService context =
        let category = CosmosStoreCategory(context, Event.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy)
        Service(streamName >> category.Resolve >> Equinox.createDecider)
