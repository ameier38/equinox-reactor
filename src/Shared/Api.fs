module Shared.Api

type AddVehicleRequest =
    { VehicleId: string
      Vehicle: Vehicle }

[<RequireQualifiedAccess>]
type AddVehicleResponse =
    | Success of string
    | VehicleAlreadyAdded of string
    | VehicleInvalid of string

type RemoveVehicleRequest =
    { VehicleId: string }

[<RequireQualifiedAccess>]
type RemoveVehicleResponse =
    | Success of string
    | VehicleNotFound of string
    | VehicleAlreadyRemoved of string

type IProcessorApi =
    { addVehicle: AddVehicleRequest -> Async<AddVehicleResponse>
      removeVehicle: RemoveVehicleRequest -> Async<RemoveVehicleResponse> }

type IReaderApi =
    { listVehicles: unit -> Async<ReadModel.VehicleOverview list>
      getVehicleCount: unit -> Async<int> }
