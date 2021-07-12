module Server.Types

open FSharp.UMX
open System

type [<Measure>] vehicleId
type VehicleId = Guid<vehicleId>

module VehicleId =
    let toString (vehicleId:VehicleId) = let value = UMX.untag vehicleId in value.ToString("N")
    let parse (s:string) =
        match Guid.TryParse(s) with
        | true, value -> UMX.tag<vehicleId> value
        | false, _ -> failwith $"Invalid VehicleId: {s}"
    let create () = Guid.NewGuid() |> UMX.tag<vehicleId>
    
type Vehicle = { make: string; model: string; year: int }
    
type InventoriedVehicle = { vehicleId: VehicleId; vehicle: Vehicle }

type Inventory = { vehicles: InventoriedVehicle[]; count: int }
