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
    
let waitForLoading () =
    let fn () =
        let countTitle = read "#count-title"
        describe $"countTitle: {countTitle}"
        not (countTitle.Contains("Loading"))
    waitFor fn
    
let addVehicle (model:string) =
    describe $"add vehicle model {model}"
    "#make" << "Toyota"
    "#model" << model
    "#year" << "2017"
    click "#submit"
    waitForLoading()
    
let countShouldBe (x:int) =
    describe $"count should be {x}"
    "#count" == $"{x}"
    
let cleanUp () =
    let mutable iteration = 1
    let mutable run = true
    while run && iteration < 10 do
        match someElement ".btn-remove" with
        | Some el ->
            click el
            waitForLoading()
        | None ->
            run <- false
        iteration <- iteration + 1

let registerTestApp (config:CanopyConfig) =
    "test app" &&& fun () ->
        let models = ["Tundra"; "Tacoma"; "Supra"]
        startApp config
        describe "should be on home page"
        on config.ClientUrl
        waitForLoading()
        let currentCount = read "#count" |> int
        for model in models do
            addVehicle model
            countShouldBe (currentCount + 1)
        describe "remove vehicles"
        cleanUp()
        countShouldBe currentCount
    
let run (browserMode:BrowserMode) =
    let mutable fail = false
    try
        let config = CanopyConfig.Load()
        printfn $"config: {config}"
        configureCanopy config
        registerTestApp config
        startBrowser browserMode
        onFail (fun _ -> fail <- true)
        run()
        quit()
        if fail then 1 else 0
    with ex ->
        printfn $"Error! {ex}"
        quit()
        1