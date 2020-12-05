module UnitTests.TestProcessor

open Expecto
open Processor
open Shared
open Serilog
open System

let log = LoggerConfiguration().WriteTo.Console().CreateLogger()
let streamConfig = StreamConfig.Load()
let memoryStore = Equinox.MemoryStore.VolatileStore()
let codec = FsCodec.NewtonsoftJson.Codec.Create<VehicleEvent>()

let resolveVehicle (vehicleId: VehicleId) =
    let resolver = Equinox.MemoryStore.Resolver(memoryStore, codec, Aggregate.fold, Aggregate.initial)
    let streamName = FsCodec.StreamName.create streamConfig.VehicleCategory (VehicleId.toStringN vehicleId)
    let vehicleStream = resolver.Resolve(streamName)
    Equinox.Stream(log, vehicleStream, maxAttempts = 3)

let streamStore =
    { new StreamStore.IStreamStore with
        member _.ResolveVehicle(vehicleId: VehicleId) = resolveVehicle (vehicleId) }

let processorApi = Api.processorApi streamStore

[<Tests>]
let testProcessor =
    testAsync "test processor" {
        // GIVEN a new vehicle
        let vehicleId = Guid.NewGuid().ToString("N")

        let req: Api.AddVehicleRequest =
            { VehicleId = vehicleId
              Vehicle =
                  { Make = "Toyota"
                    Model = "Tacoma"
                    Year = 2020 } }
        // WHEN we add the new vehicle
        let! actualRes = processorApi.addVehicle req
        // THEN the vehicle should be added successfully
        let expectedRes = Api.AddVehicleResponse.Success(sprintf "Successfully added Vehicle-%s" vehicleId)

        Expect.equal actualRes expectedRes "vehicle should be added successfully"

        // GIVEN an existing vehicle
        let req: Api.AddVehicleRequest =
            { VehicleId = vehicleId
              Vehicle =
                  { Make = "Toyota"
                    Model = "Tacoma"
                    Year = 2021 } }
        // WHEN we add the existing vehicle
        let! actualRes = processorApi.addVehicle req
        // THEN the request should fail with vehicle already added error
        let expectedRes = Api.AddVehicleResponse.VehicleAlreadyAdded (sprintf "Vehicle-%s already added" vehicleId)

        Expect.equal actualRes expectedRes "should receive vehicle already added error"

        // GIVEN an existing vehicle
        let req: Api.RemoveVehicleRequest =
            { VehicleId = vehicleId }
        // WHEN we remove the vehicle
        let! actualRes = processorApi.removeVehicle req
        // THEN the vehicle should be successfully removed
        let expectedRes = Api.RemoveVehicleResponse.Success (sprintf "Successfully removed Vehicle-%s" vehicleId)

        Expect.equal actualRes expectedRes "vehicle should be removed successfully"

        // GIVEN a vehicle that has already been removed
        let req: Api.RemoveVehicleRequest =
            { VehicleId = vehicleId }
        // WHEN we remove the vehicle
        let! actualRes = processorApi.removeVehicle req
        // THEN the request should fail with vehicle already removed error
        let expectedRes = Api.RemoveVehicleResponse.VehicleAlreadyRemoved (sprintf "Vehicle-%s already removed" vehicleId)

        Expect.equal actualRes expectedRes "should receive vehicle already removed error"
    }
