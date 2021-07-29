module Shared.Hub

open Shared.Types

[<RequireQualifiedAccess>]
type Action =
    | GetInventory
    | AddVehicle of VehicleDto
    | RemoveVehicle of VehicleId

[<RequireQualifiedAccess>]
type Response =
    | GetInventoryCompleted of InventoryDto
    | GetInventoryFailed of string
    | AddVehicleCompleted
    | AddVehicleFailed of string
    | RemoveVehicleCompleted
    | RemoveVehicleFailed of string
    
module Endpoints =
    let [<Literal>] Root = "/hub"
