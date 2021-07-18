module Shared.Hub

open Shared.Types

[<RequireQualifiedAccess>]
type Action =
    | GetInventory
    | AddVehicle of VehicleId * Vehicle
    | RemoveVehicle of VehicleId

[<RequireQualifiedAccess>]
type Response =
    | GetInventoryCompleted of Inventory
    | GetInventoryFailed of string
    | AddVehicleCompleted
    | AddVehicleFailed of string
    | RemoveVehicleCompleted
    | RemoveVehicleFailed of string
    
module Endpoints =
    let [<Literal>] Root = "/hub"
