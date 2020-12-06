module Client.App.View

open Domain
open Shared
open Elmish
open Feliz

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
                prop.onChange onChange
                prop.value value
            ]
        ]
    ]

type AddVehicleFormProps = { dispatch: Msg -> unit }

let addVehicleForm =
    React.functionComponent<AddVehicleFormProps> (fun props ->
        let make, setMake = React.useState ("")
        let model, setModel = React.useState ("")
        let year, setYear = React.useState ("")
        Html.div [
            prop.style [
                style.width 300
                style.padding 10
                style.margin 10
                style.border (2, borderStyle.solid, color.darkGray)
            ]
            prop.children [
                Html.h2 "Add Vehicle:"
                Html.form [
                    prop.onSubmit (fun e ->
                        e.preventDefault ()

                        let vehicle =
                            { Make = make
                              Model = model
                              Year = year |> int }

                        props.dispatch (Msg.AddVehicle vehicle))
                    prop.children [
                        input "make" "Make: " make setMake
                        input "model" "Model: " model setModel
                        input "year" "Year: " year setYear
                        Html.button [
                            prop.action "submit"
                            prop.text "Submit"
                        ]
                    ]
                ]
            ]
        ])

type VehicleCountProps =
    { isLoading: bool
      vehicleCount: int }

let vehicleCount =
    React.functionComponent<VehicleCountProps>(fun props ->
        Html.div [
            prop.style [
                style.width 300
                style.padding 10
                style.margin 10
                style.border (2, borderStyle.solid, color.darkGray)
            ]
            prop.children [
                Html.h2 [
                    Html.text "Vehicle Count:"
                    Html.span [
                        prop.text (if props.isLoading then " Loading..." else "")
                    ]
                ] 
                Html.h1 [
                    prop.id "count"
                    prop.style [
                        style.fontSize 60
                        style.textAlign.center
                    ]
                    prop.text props.vehicleCount
                ]
            ]
        ])

type VehiclesListProps =
    { isLoading: bool
      vehicles: ReadModel.VehicleOverview list
      dispatch: Msg -> unit }

let vehiclesList =
    React.functionComponent<VehiclesListProps> (fun props ->
        Html.div [
            prop.style [
                style.width 400
                style.padding 10
                style.margin 10
                style.border (2, borderStyle.solid, color.darkGray)
            ]
            prop.children [
                Html.h2 [
                    Html.text "Vehicles:"
                    Html.span (if props.isLoading then " Loading..." else "")
                ] 
                Html.unorderedList [
                    prop.style [
                        style.height 150
                        style.overflowY.scroll
                    ]
                    prop.children [
                        for doc in props.vehicles do
                            Html.li [
                                prop.className "vehicle"
                                prop.children [
                                    Html.button [
                                        prop.style [
                                            style.marginRight 5
                                        ]
                                        prop.text "Remove"
                                        prop.onClick (fun e ->
                                            e.preventDefault()
                                            props.dispatch (Msg.RemoveVehicle doc.VehicleId)
                                        )
                                    ]
                                    Html.span
                                        (sprintf
                                            "%i %s %s: %s"
                                             doc.Year
                                             doc.Make
                                             doc.Model
                                             (doc.Status))
                                ]
                            ]
                    ]
                ]
            ]
        ])

let render (state:State) (dispatch:Msg -> unit) =
    Html.div [
        prop.style [
            style.display.flex
            style.flexWrap.wrap
            style.justifyContent.spaceAround
        ]
        prop.children [
            addVehicleForm { dispatch = dispatch }
            vehicleCount { isLoading = state.IsVehicleCountLoading; vehicleCount = state.VehicleCount }
            vehiclesList { vehicles = state.Vehicles; isLoading = state.IsVehiclesLoading; dispatch = dispatch }
        ]
    ]
