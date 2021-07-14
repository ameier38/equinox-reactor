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
        prop.className "p-2 m-2 rounded-md border-2 border-gray-200"
        prop.children children
    ]
 
[<ReactComponent>]
let VehicleForm () =
    let make, setMake = React.useState("")
    let model, setModel = React.useState("")
    let year, setYear = React.useState("")
    let server = React.useServer()
    section [
        Html.h2 "Add Vehicle:"
        Html.form [
            prop.onSubmit(fun e ->
                e.preventDefault()
                let vehicleId = VehicleId.create()
                let vehicle = { make = make; model = model; year = int year }
                server.addVehicle(vehicleId, vehicle))
            prop.children [
                input "make" "Make: " make setMake
                input "model" "Model: " model setModel
                input "year" "Year: " year setYear
                Html.button [
                    prop.className "rounded-md shadow p-2 bg-gray-200"
                    prop.action "submit"
                    prop.text "Submit"
                    
                ]
            ]
        ]
    ]
    
[<ReactComponent>]
let VehicleCount () =
    let server = React.useServer()
    section [
        Html.h2 [
            Html.text "Vehicle Count:"
        ]
        Html.h1 [
            match server.Inventory with
            | HasNotStarted | InProgress ->
                prop.text "Loading..."
            | Resolved inventory ->
                prop.text inventory.count
            | Failed ex ->
                Log.error ex
                prop.text $"Error: {ex}"
        ]
    ]
    
[<ReactComponent>]
let VehicleList() =
    let server = React.useServer()
    section [
        Html.h2 "Vehicles:"
        match server.Inventory with
        | HasNotStarted | InProgress ->
            Html.h2 "Loading..."
        | Resolved inventory ->
            Html.unorderedList [
                prop.children [
                    for { vehicleId = vid; vehicle = v } in inventory.vehicles do
                        Html.li [
                            prop.className "my-2"
                            prop.children [
                                Html.button [
                                    prop.className "rounded-md shadow p-2 bg-gray-200 mr-2"
                                    prop.text "Remove"
                                    prop.onClick(fun e ->
                                        e.preventDefault()
                                        server.removeVehicle(vid))
                                ]
                                Html.span $"{v.year} {v.make} {v.model}"
                            ]
                        ]
                ]
            ]
        | Failed ex ->
            Html.h2 $"Error: {ex}"
    ]

[<ReactComponent>]
let Page () =
    Html.div [
        prop.className "container mx-auto flex justify-center"
        prop.children [
            VehicleForm()
            VehicleCount()
            VehicleList()
        ]
    ]

[<ReactComponent>]
let App () =
    Server.provider [
        Page()
    ]
