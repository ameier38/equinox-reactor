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
            
    let invoke (action:Action) (hubCtx:FableHub) =
        let vehicleService = hubCtx.Services.GetService<Vehicle.Service>()
        let inventoryService = hubCtx.Services.GetService<Inventory.Service>()
        let update' = update vehicleService inventoryService
        Async.StartAsTask(update' action)
        
    let send (action:Action) (hubCtx:FableHub<Action,Response>) =
        async {
            Log.Information($"action: {action}")
            let vehicleService = hubCtx.Services.GetRequiredService<Vehicle.Service>()
            let inventoryService = hubCtx.Services.GetRequiredService<Inventory.Service>()
            let update' = update vehicleService inventoryService
            let! response = update' action
            return hubCtx.Clients.Caller.Send(response)
        } |> Async.StartAsTask :> Task
            
let settings: SignalR.Settings<Action,Response> =
    { EndpointPattern = Endpoints.Root
      Send = Settings.send
      Invoke = Settings.invoke
      Config = None }
