module Processor.Api

open Shared
open Shared.Api
open StreamStore

let addVehicle
    (store: IStreamStore) =
    fun (req: AddVehicleRequest) ->
        async {
            let vehicleId = VehicleId.parse req.VehicleId
            let stream = store.ResolveVehicle(vehicleId)
            let cmd = AddVehicle req.Vehicle
            match! stream.Transact(Aggregate.decide vehicleId cmd) with
            | Ok msg -> return AddVehicleResponse.Success msg
            | Error (VehicleAlreadyAdded msg) -> return AddVehicleResponse.VehicleAlreadyAdded msg
            | Error err -> return failwithf "Error! %A" err
        }

let removeVehicle
    (store: IStreamStore) =
    fun (req: RemoveVehicleRequest) ->
        async {
            let vehicleId = VehicleId.parse req.VehicleId
            let stream = store.ResolveVehicle(vehicleId)
            let cmd = RemoveVehicle
            match! stream.Transact(Aggregate.decide vehicleId cmd) with
            | Ok msg -> return RemoveVehicleResponse.Success msg
            | Error (VehicleNotFound msg) -> return RemoveVehicleResponse.VehicleNotFound msg
            | Error (VehicleAlreadyRemoved msg) -> return RemoveVehicleResponse.VehicleAlreadyRemoved msg
            | Error err -> return failwithf "Error! %A" err
        }

let processorApi
    (store: IStreamStore)
    : IProcessorApi =
    { addVehicle = addVehicle store
      removeVehicle = removeVehicle store }
