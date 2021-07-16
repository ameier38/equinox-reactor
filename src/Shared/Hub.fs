module Shared.Hub

open Shared.Types

[<RequireQualifiedAccess>]
type Action =
    | GetInventory
    | AddVehicle of VehicleId * Vehicle
    | RemoveVehicle of VehicleId

[<RequireQualifiedAccess>]
type Response =
    | CommandSucceeded
    | InventoryUpdated of Inventory
    
module Endpoints =
    let [<Literal>] Root = "/hub"
