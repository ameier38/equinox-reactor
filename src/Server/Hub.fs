module Server.Hub

open Fable.SignalR
open Microsoft.Extensions.DependencyInjection
open Shared.Hub
open Serilog
open System.Threading.Tasks

module Settings =
    let update (vehicleService:Vehicle.Service) (inventoryService:Inventory.Service) =
        fun (action:Action) ->
            async {
                match action with
                | Action.GetInventory ->
                    let! inventory = inventoryService.Read()
                    return Response.InventoryUpdated inventory
                | Action.AddVehicle (vehicleId, vehicle) ->
                    do! vehicleService.AddVehicle(vehicleId, vehicle)
                    return Response.CommandSucceeded
                | Action.RemoveVehicle vehicleId ->
                    do! vehicleService.RemoveVehicle(vehicleId)
                    return Response.CommandSucceeded
            }
            
    let invoke (vehicleService:Vehicle.Service) (inventoryService:Inventory.Service) =
        fun (action:Action) (_hubCtx:FableHub) ->
            let update' = update vehicleService inventoryService
            Async.StartAsTask(update' action)
        
    let send (vehicleService:Vehicle.Service) (inventoryService:Inventory.Service) =
        fun (action:Action) (hubCtx:FableHub<Action,Response>) ->
            async {
                let update' = update vehicleService inventoryService
                let! response = update' action
                return hubCtx.Clients.Caller.Send(response)
            } |> Async.StartAsTask :> Task
            
let createSettings
        (vehicleService:Vehicle.Service)
        (inventoryService:Inventory.Service)
    : SignalR.Settings<Action,Response> =
    { EndpointPattern = Endpoints.Root
      Send = Settings.send vehicleService inventoryService
      Invoke = Settings.invoke vehicleService inventoryService
      Config = None }
