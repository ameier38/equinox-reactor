module Client.App.State

open Browser
open Client.Api
open Domain
open Elmish
open Shared
open Shared.Api
open System

let loadVehicles () =
    async {
        try
            return! readerApi.listVehicles()
        with ex ->
            console.error ex
            return raise ex
    }

let reloadVehicles () =
    async {
        do! Async.Sleep 500
        return! loadVehicles()
    }

let loadVehicleCount () =
    async {
        try
            return! readerApi.getVehicleCount()
        with ex ->
            console.error ex
            return raise ex
    }

let reloadVehicleCount () =
    async {
        do! Async.Sleep 500
        return! loadVehicleCount()
    }

let addVehicle (vehicle:Vehicle) =
    async {
        try
            let vehicleId = Guid.NewGuid().ToString("N")
            let req: AddVehicleRequest =
                { VehicleId = vehicleId
                  Vehicle = vehicle }
            match! processorApi.addVehicle(req) with
            | AddVehicleResponse.Success msg ->
                return msg
            | err ->
                console.error err
                return failwithf "Error! %A" err
        with ex ->
            console.error ex
            return raise ex
    }

let removeVehicle (vehicleId:string) =
    async {
        try
            let req: RemoveVehicleRequest =
                { VehicleId = vehicleId }
            match! processorApi.removeVehicle(req) with
            | RemoveVehicleResponse.Success msg ->
                return msg
            | err ->
                console.error err
                return failwithf "Error! %A" err
        with ex ->
            console.error ex
            return raise ex
    }

let init () =
    let loadVehiclesCmd = Cmd.OfAsync.either loadVehicles () VehiclesLoaded ErrorReceived
    let loadVehicleCountCmd = Cmd.OfAsync.either loadVehicleCount () VehicleCountLoaded ErrorReceived
    { IsVehiclesLoading = true
      IsVehicleCountLoading = true
      Vehicles = []
      VehicleCount = 0 },
    Cmd.batch [loadVehiclesCmd; loadVehicleCountCmd]

let update (msg: Msg) (state: State) =
    match msg with
    | LoadVehicles ->
        { state with IsVehiclesLoading = true },
        Cmd.OfAsync.either loadVehicles () VehiclesLoaded ErrorReceived
    | LoadVehicleCount ->
        { state with IsVehicleCountLoading = true },
        Cmd.OfAsync.either loadVehicleCount () VehicleCountLoaded ErrorReceived
    | Msg.AddVehicle vehicle ->
        { state with
            IsVehiclesLoading = true
            IsVehicleCountLoading = true },
        Cmd.OfAsync.either addVehicle vehicle Msg.VehicleAdded ErrorReceived
    | Msg.RemoveVehicle vehicleId ->
        { state with
            IsVehiclesLoading = true
            IsVehicleCountLoading = true },
        Cmd.OfAsync.either removeVehicle vehicleId Msg.VehicleRemoved ErrorReceived
    | VehiclesLoaded vehicles ->
        { state with
            IsVehiclesLoading = false
            Vehicles = vehicles },
        Cmd.none
    | VehicleCountLoaded vehicleCount ->
        { state with
            IsVehicleCountLoading = false
            VehicleCount = vehicleCount },
        Cmd.none
    | Msg.VehicleAdded _
    | Msg.VehicleRemoved _ ->
        let reloadVehiclesCmd = Cmd.OfAsync.either reloadVehicles () VehiclesLoaded ErrorReceived
        let reloadVehicleCountCmd = Cmd.OfAsync.either reloadVehicleCount () VehicleCountLoaded ErrorReceived
        state, Cmd.batch [reloadVehiclesCmd; reloadVehicleCountCmd]
    | ErrorReceived err ->
        console.error err
        state, Cmd.none
