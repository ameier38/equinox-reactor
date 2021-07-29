module Tests.IntegrationTests

open canopy.classic
open canopy.runner.classic
open canopy.types
open OpenQA.Selenium.Chrome
open Config

[<RequireQualifiedAccess>]
type BrowserMode =
    | Local
    | Headless

let configureCanopy (config:CanopyConfig) =
    canopy.configuration.chromeDir <- config.ChromeDriverDir
    canopy.configuration.failScreenshotPath <- config.ScreenshotsDir
    canopy.configuration.failureScreenshotsEnabled <- true

let startBrowser (browserMode:BrowserMode) =
    let browserStartMode =
        let chromeOptions = ChromeOptions()
        chromeOptions.AddArgument("--no-sandbox")
        match browserMode with
        | BrowserMode.Local -> ()
        | BrowserMode.Headless ->
            chromeOptions.AddArgument("--headless")
        ChromeWithOptions chromeOptions
    start browserStartMode
    pin Left 
    resize (1000, 600)

let startApp (config:CanopyConfig) =
    let clientUrl = config.ClientUrl
    describe $"starting app {clientUrl}"
    url clientUrl
    waitForElement "#app"
    
let loading () =
    let countTitle = read "#count-title"
    not (countTitle.Contains("Loading"))
    

let registerTestApp (config:CanopyConfig) =
    "test app" &&& fun () ->
        startApp config
        describe "should be on home page"
        on config.ClientUrl
        waitFor loading
        let initialCount = read "#count" |> int
        describe $"initial count: {initialCount}"
        "#make" << "Toyota"
        "#model" << "Tundra"
        "#year" << "2001"
        click "#submit"
        waitFor loading
        describe $"count should be {initialCount + 1}"
        "#count" == $"{initialCount + 1}"
        describe "remove vehicle"
        let removeButton = elementWithText "button" "Remove"
        click removeButton
        waitFor loading
        describe $"count should be {initialCount}"
        "#count" == $"{initialCount}"
    
let run (browserMode:BrowserMode) =
    let config = CanopyConfig.Load()
    let mutable fail = false
    printfn $"config: {config}"
    configureCanopy config
    registerTestApp config
    startBrowser browserMode
    onFail (fun _ -> fail <- true)
    run()
    quit()
    if fail then 1 else 0
    