module Processor.Api

open Shared
open StreamStore

type IProcessorApi =
    { addVehicle: VehicleId * Vehicle -> Async<Result<string, VehicleError>>
      removeVehicle: VehicleId -> Async<Result<string, VehicleError>> }

let addVehicle
    (store: EventStoreDBStreamStore) =
    fun (vehicleId:VehicleId, vehicle:Vehicle) ->
        async {
            let stream = store.ResolveVehicle(vehicleId)
            let cmd = AddVehicle vehicle
            return! stream.Transact(Aggregate.decide vehicleId cmd)
        }

let removeVehicle
    (store: EventStoreDBStreamStore) =
    fun (vehicleId:VehicleId) ->
        async {
            let stream = store.ResolveVehicle(vehicleId)
            let cmd = RemoveVehicle
            return! stream.Transact(Aggregate.decide vehicleId cmd)
        }

let processorApi
    (store: EventStoreDBStreamStore)
    : IProcessorApi =
    { addVehicle = addVehicle store
      removeVehicle = removeVehicle store }
