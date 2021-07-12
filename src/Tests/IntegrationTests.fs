module Tests.IntegrationTests

open canopy.classic
open canopy.runner.classic
open canopy.types
open OpenQA.Selenium.Chrome

let canopyConfig = Config.CanopyConfig.Load()

canopy.configuration.chromeDir <- canopyConfig.DriverDir
canopy.configuration.webdriverPort <- Some canopyConfig.DriverPort
canopy.configuration.failScreenshotPath <- canopyConfig.ScreenshotDir
canopy.configuration.failureScreenshotsEnabled <- true

type StartMode =
    | Headfull
    | Headless

let startBrowser (startMode:StartMode) =
    let browserStartMode =
        let chromeOptions = ChromeOptions()
        chromeOptions.AddArgument("--no-sandbox")
        match startMode with
        | Headfull -> ChromeWithOptions chromeOptions
        | Headless ->
            let chromeOptions = ChromeOptions()
            chromeOptions.AddArgument("--no-sandbox")
            chromeOptions.AddArgument("--headless")
            Remote(canopyConfig.DriverUrl, chromeOptions.ToCapabilities())
    start browserStartMode
    pin Left 
    resize (1000, 600)


let startApp () =
    url canopyConfig.ClientUrl
    waitForElement "#app"

let addVehicle year make model =
    describe (sprintf "adding vehicle %s %s %s" year make model)
    "#make" << make
    "#model" << model
    "#year" << year
    click "Submit"

let removeVehicle () =
    click (first "Remove")

"test add vehicle" &&& fun _ ->
    startApp()
    sleep 10
    let vehicleCount = read "#count" |> int
    describe (sprintf "vehicle count should be %i" vehicleCount)
    count ".vehicle" vehicleCount
    addVehicle "2020" "Toyota" "Tacoma"
    describe (sprintf "vehicle count should be %i" (vehicleCount + 1))
    "#count" == (vehicleCount + 1 |> string)
    count ".vehicle" (vehicleCount + 1)
    sleep 2
    addVehicle "2021" "Tesla" "Cybertruck"
    describe (sprintf "vehicle count should be %i" (vehicleCount + 2))
    "#count" == (vehicleCount + 2 |> string)
    count ".vehicle" (vehicleCount + 2)
    removeVehicle()
    sleep 10
