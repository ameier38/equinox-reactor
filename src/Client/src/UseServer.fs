module Client.UseServer

open Elmish
open Fable.SignalR
open Fable.SignalR.Elmish
open Feliz
open Feliz.UseElmish
open Shared.Types
open Shared.Hub

type State =
    { Inventory: Inventory
      GetInventory: Deferred
      AddVehicle: Deferred
      RemoveVehicle: Deferred
      Hub: Elmish.Hub<Action,Response> option }
    
type Msg =
    | RegisterHub of Elmish.Hub<Action,Response>
    | Action of Action
    | Response of Response
    
let init() =
    { Inventory = { vehicles = Array.empty }
      GetInventory = HasNotStarted
      AddVehicle = HasNotStarted
      RemoveVehicle = HasNotStarted
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
    | Response (Response.GetInventoryCompleted inventory) ->
        { state with
            Inventory = inventory
            GetInventory = Resolved },
        Cmd.none
    | Response (Response.GetInventoryFailed message) ->
        { state with
            GetInventory = Failed message },
        Cmd.none
    | Response Response.AddVehicleCompleted ->
        { state with AddVehicle = Resolved },
        Cmd.none
    | Response (Response.AddVehicleFailed message) ->
        { state with AddVehicle = Failed message },
        Cmd.none
    | Response Response.RemoveVehicleCompleted ->
        { state with RemoveVehicle = Resolved },
        Cmd.none
    | Response (Response.RemoveVehicleFailed message) ->
        { state with RemoveVehicle = Failed message },
        Cmd.none
    | Action Action.GetInventory ->
        { state with GetInventory = InProgress },
        Cmd.SignalR.send state.Hub Action.GetInventory
    | Action (Action.AddVehicle vehicle) ->
        { state with
            // NB: optimistically add the vehicle
            Inventory =
                { state.Inventory with
                    vehicles = state.Inventory.vehicles |> Array.append [| vehicle |] }
            GetInventory = InProgress
            AddVehicle = InProgress },
        Cmd.SignalR.send state.Hub (Action.AddVehicle(vehicle))
    | Action (Action.RemoveVehicle vehicleId) ->
        { state with
            // NB: optimistically remove the vehicle
            Inventory =
                { state.Inventory with
                    vehicles = state.Inventory.vehicles |> Array.filter (fun v -> v.vehicleId <> vehicleId) }
            GetInventory = InProgress
            RemoveVehicle = InProgress },
        Cmd.SignalR.send state.Hub (Action.RemoveVehicle(vehicleId))

type ServerProviderValue =
    { Inventory: Inventory
      GetInventory: Deferred
      AddVehicle: Deferred
      RemoveVehicle: Deferred
      addVehicle: Vehicle -> unit
      removeVehicle: VehicleId -> unit }
    
module ServerProvider =
    let serverContext = React.createContext<ServerProviderValue>("ServerContext")
    
    [<ReactComponent>]
    let ServerProvider (children:seq<ReactElement>) =
        let state, dispatch = React.useElmish(init, update)
        let providerValue =
            { Inventory = state.Inventory
              GetInventory = state.GetInventory
              AddVehicle = state.AddVehicle
              RemoveVehicle = state.RemoveVehicle
              addVehicle = fun payload -> dispatch (Action (Action.AddVehicle payload))
              removeVehicle = fun payload -> dispatch (Action (Action.RemoveVehicle payload)) }
        React.contextProvider(serverContext, providerValue, children)
        
type Server =
    static member inline provider (children:seq<ReactElement>): ReactElement =
        ServerProvider.ServerProvider(children)
        
module React =
    let useServer() = React.useContext(ServerProvider.serverContext)
