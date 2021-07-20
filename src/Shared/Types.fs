module Shared.Types

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

type Vehicle = { vehicleId: VehicleId; make: string; model: string; year: int }
type Inventory = { vehicles: Vehicle[] }
