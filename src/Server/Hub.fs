module Server.Hub

open System
open Fable.SignalR
open Microsoft.Extensions.DependencyInjection
open Shared.Hub
open Serilog
open System.Threading.Tasks

module Settings =
    let update (services:IServiceProvider) =
        fun (action:Action) ->
            async {
                let vehicleService = services.GetRequiredService<Vehicle.Service>()
                let inventoryService = services.GetRequiredService<Inventory.Service>()
                match action with
                | Action.GetInventory ->
                    try
                        let! inventory = inventoryService.Read()
                        return Response.GetInventoryCompleted inventory
                    with ex ->
                        return Response.GetInventoryFailed ex.Message
                | Action.AddVehicle vehicleDto ->
                    try
                        do! vehicleService.Add(vehicleDto)
                        return Response.AddVehicleCompleted
                    with ex ->
                        return Response.AddVehicleFailed ex.Message
                | Action.RemoveVehicle vehicleId ->
                    try
                        do! vehicleService.Remove(vehicleId)
                        return Response.RemoveVehicleCompleted
                    with ex ->
                        return Response.RemoveVehicleFailed ex.Message
            }
            
    let invoke (action:Action) (hubCtx:FableHub) =
            let update' = update hubCtx.Services
            Async.StartAsTask(update' action)
        
    let send (action:Action) (hubCtx:FableHub<Action,Response>) =
            async {
                let update' = update hubCtx.Services
                let! response = update' action
                return hubCtx.Clients.Caller.Send(response)
            } |> Async.StartAsTask :> Task
            
let settings
    : SignalR.Settings<Action,Response> =
    { EndpointPattern = Endpoints.Root
      Send = Settings.send
      Invoke = Settings.invoke
      Config = None }
