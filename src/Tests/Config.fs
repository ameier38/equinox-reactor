module Tests.Config

open System
open System.IO

type CanopyConfig =
    { ClientUrl: string
      ChromeDriverDir: string
      ScreenshotsDir: string }
    static member Load() =
        let clientScheme = Env.getVariable "CLIENT_SCHEME" "http"
        let clientHost = Env.getVariable "CLIENT_HOST" "localhost"
        let clientPort = Env.getVariable "CLIENT_PORT" "5000" |> int
        { ClientUrl = $"{clientScheme}://{clientHost}:{clientPort}"
          ChromeDriverDir = Env.getVariable "CHROME_DRIVER_DIR" AppContext.BaseDirectory
          ScreenshotsDir = Env.getVariable "SCREENSHOTS_DIR" (Path.Join(AppContext.BaseDirectory, "screenshots")) }
