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

let registerTestApp (config:CanopyConfig) =
    "test app" &&& fun () ->
        startApp config
        describe "should be on home page"
    
let run (browserMode:BrowserMode) =
    let mutable failed = false
    try
        let config = CanopyConfig.Load()
        printfn $"config: {config}"
        configureCanopy config
        registerTestApp config
        startBrowser browserMode
        run()
        onFail (fun _ -> failed <- true)
        quit()
        if failed then 1 else 0
    with ex ->
        printfn $"Error! {ex}"
        quit()
        1