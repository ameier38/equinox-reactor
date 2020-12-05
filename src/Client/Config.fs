module Client.Config

open Shared

type ProcessorConfig =
    { Url: string }
    static member Load() =
        let scheme = Env.getEnv "PROCESSOR_SCHEME" "http"
        let host = Env.getEnv "PROCESSOR_HOST" "localhost"
        let port = Env.getEnv "PROCESSOR_PORT" "5001" |> int
        { Url = sprintf "%s://%s:%i" scheme host port }

type ReaderConfig =
    { Url: string }
    static member Load() =
        let scheme = Env.getEnv "READER_SCHEME" "http"
        let host = Env.getEnv "READER_HOST" "localhost"
        let port = Env.getEnv "READER_PORT" "5002" |> int
        { Url = sprintf "%s://%s:%i" scheme host port }
