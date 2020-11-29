namespace Shared

type Vehicle =
    { Make: string
      Model: string
      Year: int }

type VehicleCommand =
    | AddVehicle of Vehicle
    | RemoveVehicle

type VehicleEvent =
    | VehicleAdded of {| VehicleId: VehicleId; Vehicle: Vehicle |}
    | VehicleRemoved of {| VehicleId: VehicleId |}
    interface TypeShape.UnionContract.IUnionContract

type VehicleState =
    | VehicleStateUnknown
    | VehicleAvailable
    | VehicleRemoved

type VehicleError =
    | VehicleNotFound of string
    | VehicleAlreadyAdded of string
    | VehicleAlreadyRemoved of string
