module Client.App.Domain

open Shared

type State =
    { IsVehiclesLoading: bool
      IsVehicleCountLoading: bool
      Vehicles: ReadModel.VehicleOverview list
      VehicleCount: int }

type Msg =
    | LoadVehicles
    | LoadVehicleCount
    | AddVehicle of Vehicle
    | RemoveVehicle of string
    | VehiclesLoaded of ReadModel.VehicleOverview list
    | VehicleCountLoaded of int
    | VehicleAdded of string
    | VehicleRemoved of string
    | ErrorReceived of exn
