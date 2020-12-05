module Config

open Shared
open System

type ClientConfig =
    { Url: string }
    static member Load() =
        let scheme = Env.getEnv "CLIENT_SCHEME" "http"
        let host = Env.getEnv "CLIENT_HOST" "localhost"
        let port = Env.getEnv "CLIENT_PORT" "3000" |> int
        { Url = sprintf "%s://%s:%i" scheme host port }

type ChromeConfig =
    { DriverUrl: string
      DriverDir: string
      DriverPort: int }
    static member Load() =
        let scheme = Env.getEnv "CHROME_SCHEME" "http"
        let host = Env.getEnv "CHROME_HOST" "localhost"
        let port = Env.getEnv "CHROME_PORT" "3001" |> int
        { DriverUrl = sprintf "%s://%s:%i/webdriver" scheme host port
          DriverDir = Env.getEnv "CHROME_DRIVER_DIR" AppContext.BaseDirectory
          DriverPort = 4444 }
