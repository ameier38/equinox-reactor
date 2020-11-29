module Processor.Aggregate

open Shared

let initial = VehicleStateUnknown

let evolve (_:VehicleState) (event:VehicleEvent): VehicleState =
    match event with
    | VehicleAdded _ -> VehicleAvailable
    | VehicleEvent.VehicleRemoved _  -> VehicleRemoved

let decide
    (vehicleId: VehicleId)
    : VehicleCommand -> VehicleState -> Result<string, VehicleError> * VehicleEvent list =
    fun (command:VehicleCommand) (state:VehicleState) ->
        let vehicleIdStr = VehicleId.toStringN vehicleId
        match command with
        | AddVehicle vehicle ->
            match state with
            | VehicleStateUnknown
            | VehicleRemoved ->
                let vehicleAdded = VehicleAdded {| VehicleId = vehicleId; Vehicle = vehicle |}
                let msg = sprintf "Successfully added Vehicle-%s" vehicleIdStr
                Ok msg, [vehicleAdded]
            | VehicleAvailable ->
                let msg = sprintf "Vehicle-%s already added" vehicleIdStr
                Error (VehicleAlreadyAdded msg), []
        | RemoveVehicle ->
            match state with
            | VehicleStateUnknown ->
                let msg = sprintf "Vehicle-%s not found" vehicleIdStr
                Error (VehicleNotFound msg), []
            | VehicleRemoved ->
                let msg = sprintf "Vehicle-%s already removed" vehicleIdStr
                Error (VehicleAlreadyRemoved msg), []
            | VehicleAvailable ->
                let msg = sprintf "Successfully removed Vehicle-%s" vehicleIdStr
                let vehicleRemoved = VehicleEvent.VehicleRemoved {| VehicleId = vehicleId |}
                Ok msg, [vehicleRemoved]

let fold: VehicleState -> seq<VehicleEvent> -> VehicleState = Seq.fold evolve
