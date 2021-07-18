module Tests.Config

open System
open System.IO

type CanopyConfig =
    { ClientUrl: string
      ChromeDriverDir: string
      ScreenshotsDir: string }
    static member Load() =
        let clientScheme = Env.getEnv "CLIENT_SCHEME" "http"
        let clientHost = Env.getEnv "CLIENT_HOST" "localhost"
        let clientPort = Env.getEnv "CLIENT_PORT" "5000" |> int
        { ClientUrl = $"{clientScheme}://{clientHost}:{clientPort}"
          ChromeDriverDir = Env.getEnv "CHROME_DRIVER_DIR" AppContext.BaseDirectory
          ScreenshotsDir = Env.getEnv "SCREENSHOTS_DIR" (Path.Join(AppContext.BaseDirectory, "screenshots")) }
