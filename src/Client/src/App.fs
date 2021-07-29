module Client.App

open Client.UseServer
open Feliz
open Shared.Types

let input (id:string) (label:string) (value:string) (onChange: string -> unit) =
    Html.div [
        prop.style [
            style.display.block
            style.padding 5
        ]
        prop.children [
            Html.label [
                prop.style [
                    style.display.inlineBlock
                    style.width 60
                ]
                prop.htmlFor id
                prop.text label
            ]
            Html.input [
                prop.id id
                prop.type' "text"
                prop.onChange onChange
                prop.value value
            ]
        ]
    ]
    
let section (children:seq<ReactElement>) =
    Html.div [
        prop.children children
    ]
 
[<ReactComponent>]
let VehicleForm () =
    let make, setMake = React.useState("")
    let model, setModel = React.useState("")
    let year, setYear = React.useState("")
    let server = React.useServer()
    let disabled =
        match server.AddVehicle with
        | InProgress -> "disabled"
        | _ -> ""
    let clearForm () =
        match server.AddVehicle with
        | Resolved -> setMake ""; setModel ""; setYear ""
        | _ -> ()
    React.useEffect(clearForm, [| box server.AddVehicle |])
    Html.div [
        prop.className "w-1/2 p-2 m-2 rounded-md border-2 border-gray-200"
        prop.children [
            Html.h2 "Add Vehicle:"
            Html.form [
                prop.onSubmit(fun e ->
                    e.preventDefault()
                    let vehicleId = VehicleId.create()
                    let vehicle = { vehicleId = vehicleId; make = make; model = model; year = int year }
                    server.addVehicle(vehicle))
                prop.children [
                    input "make" "Make: " make setMake
                    input "model" "Model: " model setModel
                    input "year" "Year: " year setYear
                    Html.button [
                        prop.id "submit"
                        prop.className $"rounded-md shadow p-2 bg-gray-200 {disabled}"
                        prop.action "submit"
                        prop.text "Submit"
                    ]
                ]
            ]
        ]
    ]
    
[<ReactComponent>]
let VehicleCount () =
    let server = React.useServer()
    Html.div [
        prop.className "w-1/2 p-2 m-2 rounded-md border-2 border-gray-200"
        prop.children [
            Html.h2 [
                prop.id "count-title"
                prop.children [
                    Html.text "Vehicle Count: "
                    match server.GetInventory with
                    | HasNotStarted | InProgress ->
                        Html.text "Loading..."
                    | Resolved ->
                        Html.text ""
                    | Failed ex ->
                        Log.error ex
                        Html.text $"Error: {ex}"
                ]
            ]
            Html.h1 [
                prop.id "count"
                prop.className "text-xl font-medium"
                prop.text server.Inventory.vehicles.Length
            ]
        ]
    ]
    
[<ReactComponent>]
let VehicleList() =
    let server = React.useServer()
    Html.div [
        prop.className "w-full p-2 m-2 rounded-md border-2 border-gray-200"
        prop.children[
            Html.h2 [
                prop.children [
                    Html.text "Vehicles: "
                    match server.GetInventory with
                    | HasNotStarted | InProgress ->
                        Html.text "Loading..."
                    | Resolved ->
                        Html.text ""
                    | Failed ex ->
                        Log.error ex
                        Html.text $"Error: {ex}"
                ]
            ]
            Html.unorderedList [
                prop.children [
                    for vehicle in server.Inventory.vehicles do
                        Html.li [
                            prop.className "my-2 vehicle"
                            prop.children [
                                Html.button [
                                    prop.className "btn-remove rounded-md shadow p-2 bg-gray-200 mr-2"
                                    prop.text "Remove"
                                    prop.onClick(fun e ->
                                        e.preventDefault()
                                        server.removeVehicle(vehicle.vehicleId))
                                ]
                                Html.span $"{vehicle.year} {vehicle.make} {vehicle.model}"
                            ]
                        ]
                ]
            ]
        ]
    ]

[<ReactComponent>]
let Page () =
    Html.div [
        prop.className "container mx-auto"
        prop.children [
            Html.div [
                prop.className "w-full flex"
                prop.children [
                    VehicleForm()
                    VehicleCount()
                ]
            ]
            Html.div [
                prop.className "w-full flex"
                prop.children [
                    VehicleList()
                ]
            ]
        ]
    ]

[<ReactComponent>]
let App () =
    Server.provider [
        Page()
    ]
