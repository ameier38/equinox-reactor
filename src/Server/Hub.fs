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
                let vehicleService = services.GetRequiredService<Domain.Vehicle.Service>()
                let inventoryService = services.GetRequiredService<Domain.Inventory.Service>()
                Log.Information("received action {Action}", action)
                match action with
                | Action.GetInventory ->
                    try
                        let! inventory = inventoryService.Read()
                        return Response.GetInventoryCompleted inventory
                    with ex ->
                        return Response.GetInventoryFailed ex.Message
                | Action.AddVehicle vehicle ->
                    try
                        let domainVehicle:Domain.Vehicle.Events.Vehicle =
                            { make = vehicle.make; model = vehicle.model; year= vehicle.year }
                        do! vehicleService.Add(vehicle.vehicleId, domainVehicle)
                        return Response.AddVehicleCompleted
                    with ex ->
                        return Response.AddVehicleFailed ex.Message
                | Action.RemoveVehicle vehicleId ->
                    try
                        do! vehicleService.Remove(vehicleId)
                        return Response.RemoveVehicleCompleted
                    with ex ->
                        return Response.RemoveVehicleFailed ex.Message
                | Action.InventoryUpdated ->
                    try
                        let! inventory = inventoryService.Read()
                        return Response.GetInventoryCompleted inventory
                    with ex ->
                        return Response.GetInventoryFailed ex.Message
            }
            
    let invoke (action:Action) (hubCtx:FableHub) =
            let update' = update hubCtx.Services
            Async.StartAsTask(update' action)
        
    let send (action:Action) (hubCtx:FableHub<Action,Response>) =
            async {
                let update' = update hubCtx.Services
                let! response = update' action
                Log.Information("sending response {Response}", response)
                return hubCtx.Clients.All.Send(response)
            } |> Async.StartAsTask :> Task
            
let settings
    : SignalR.Settings<Action,Response> =
    { EndpointPattern = Endpoints.Root
      Send = Settings.send
      Invoke = Settings.invoke
      Config = None }
