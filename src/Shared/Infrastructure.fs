namespace Shared

open FSharp.UMX
open System

type [<Measure>] vehicleId
type VehicleId = Guid<vehicleId>
module VehicleId =
    let toStringN (vehicleId:VehicleId) = let value = UMX.untag vehicleId in value.ToString("N")
    let parse (s:string) =
        match Guid.TryParse(s) with
        | true, value -> UMX.tag<vehicleId> value
        | false, _ -> failwithf "Invalid VehicleId: %s" s
    let create () = Guid.NewGuid() |> UMX.tag<vehicleId>

module Env =
    let getEnv (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrEmpty(value) -> defaultValue
        | value -> value
