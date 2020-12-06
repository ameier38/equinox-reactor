module Config

open Shared
open System
open System.IO

type CanopyConfig =
    { ClientUrl: string
      DriverUrl: string
      DriverDir: string
      DriverPort: int
      ScreenshotDir: string }
    static member Load() =
        let clientScheme = Env.getEnv "CLIENT_SCHEME" "http"
        let clientHost = Env.getEnv "CLIENT_HOST" "localhost"
        let clientPort = Env.getEnv "CLIENT_PORT" "3000" |> int
        let chromeScheme = Env.getEnv "CHROME_SCHEME" "http"
        let chromeHost = Env.getEnv "CHROME_HOST" "localhost"
        let chromePort = Env.getEnv "CHROME_PORT" "3001" |> int
        { ClientUrl = sprintf "%s://%s:%i" clientScheme clientHost clientPort
          DriverUrl = sprintf "%s://%s:%i/webdriver" chromeScheme chromeHost chromePort
          DriverDir = Env.getEnv "CHROME_DRIVER_DIR" AppContext.BaseDirectory
          DriverPort = 4444
          ScreenshotDir = Env.getEnv "SCREENSHOT_DIR" (Path.Join(AppContext.BaseDirectory, "screenshots")) }
