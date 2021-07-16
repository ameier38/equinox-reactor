module Client.UseServer

open Elmish
open Fable.SignalR
open Fable.SignalR.Elmish
open Feliz
open Feliz.UseElmish
open Shared.Types
open Shared.Hub

type State =
    { Inventory: Deferred<Inventory>
      Command: Deferred<unit>
      Hub: Elmish.Hub<Action,Response> option }
    
type Msg =
    | RegisterHub of Elmish.Hub<Action,Response>
    | Response of Response
    | GetInventory
    | AddVehicle of VehicleId * Vehicle
    | RemoveVehicle of VehicleId
    
let init() =
    { Inventory = HasNotStarted
      Command = HasNotStarted
      Hub = None },
    Cmd.SignalR.connect RegisterHub (fun hub ->
        hub.withUrl(Endpoints.Root)
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .onMessage(Response))
    
let update (msg:Msg) (state:State) =
    Log.debug $"msg: {msg}"
    match msg with
    | RegisterHub hub ->
        { state with Hub = Some hub },
        Cmd.SignalR.send (Some hub) Action.GetInventory
    | Response (Response.InventoryUpdated inventory) ->
        { state with Inventory = Resolved inventory },
        Cmd.none
    | Response Response.CommandSucceeded ->
        { state with Command = Resolved () },
        Cmd.none
    | GetInventory ->
        { state with Inventory = InProgress },
        Cmd.SignalR.send state.Hub Action.GetInventory
    | AddVehicle (vehicleId, vehicle) ->
        { state with
            Inventory = InProgress
            Command = InProgress },
        Cmd.SignalR.send state.Hub (Action.AddVehicle(vehicleId, vehicle))
    | RemoveVehicle vehicleId ->
        { state with
            Inventory = InProgress
            Command = InProgress },
        Cmd.SignalR.send state.Hub (Action.RemoveVehicle(vehicleId))

type ServerProviderValue =
    { Inventory: Deferred<Inventory>
      Command: Deferred<unit>
      addVehicle: VehicleId * Vehicle -> unit
      removeVehicle: VehicleId -> unit }
    
module ServerProvider =
    let serverContext = React.createContext<ServerProviderValue>("ServerContext")
    
    [<ReactComponent>]
    let ServerProvider (children:seq<ReactElement>) =
        let state, dispatch = React.useElmish(init, update)
        let providerValue =
            { Inventory = state.Inventory
              Command = state.Command
              addVehicle = fun payload -> dispatch (AddVehicle payload)
              removeVehicle = fun payload -> dispatch (RemoveVehicle payload) }
        React.contextProvider(serverContext, providerValue, children)
        
type Server =
    static member inline provider (children:seq<ReactElement>): ReactElement =
        ServerProvider.ServerProvider(children)
        
module React =
    let useServer() = React.useContext(ServerProvider.serverContext)
