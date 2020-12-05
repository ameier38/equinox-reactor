open canopy.classic
open canopy.runner.classic
open canopy.types
open OpenQA.Selenium.Chrome

let chromeConfig = Config.ChromeConfig.Load()
let clientConfig = Config.ClientConfig.Load()

canopy.configuration.chromeDir <- chromeConfig.DriverDir
canopy.configuration.webdriverPort <- Some chromeConfig.DriverPort

type StartMode =
    | Headfull
    | Headless

let startBrowser (startMode:StartMode) =
    let browserStartMode =
        match startMode with
        | Headfull -> Chrome
        | Headless ->
            let chromeOptions = ChromeOptions()
            chromeOptions.AddArgument("--no-sandbox")
            chromeOptions.AddArgument("--headless")
            Remote(chromeConfig.DriverUrl, chromeOptions.ToCapabilities())
    start browserStartMode

let startApp () =
    url clientConfig.Url
    waitForElement "#app"

"test add vehicle" &&& fun _ ->
    startApp()
    describe "count vehicles"
    let vehicleCount = read "#count" |> int
    count ".vehicle" vehicleCount
    describe "add vehicle"
    "#make" << "Toyota"
    "#model" << "Tacoma"
    "#year" << "2020"
    click "Submit"
    "#count" == (vehicleCount + 1 |> string)
    count ".vehicleCount" (vehicleCount + 1)


[<EntryPoint>]
let main argv =
    let startMode =
        match argv with
        | [|"--headless"|] -> Headless
        | _ -> Headfull
    try
        startBrowser startMode
        run()
        quit()
        0
    with ex ->
        printfn "Error! %A" ex
        quit()
        1
