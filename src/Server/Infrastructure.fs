namespace Server

open System
open System.IO

[<RequireQualifiedAccess>]
module Env =
    let getVariable (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | value when String.IsNullOrEmpty(value) -> defaultValue
        | value -> value
    let getSecret (name:string) (key:string) (defaultEnv:string) (defaultValue:string) =
        let secretsDir = getVariable "SECRETS_DIR" "/var/secrets"
        let secretPath = Path.Join(secretsDir, name, key)
        if File.Exists(secretPath) then
            File.ReadAllText(secretPath).Trim()
        else
            getVariable defaultEnv defaultValue
            
module Log =
    let forMetrics () =
        Serilog.Log.ForContext("isMetric", true)

module Equinox =
    let createDecider stream =
        Equinox.Decider(Log.forMetrics (), stream, maxAttempts = 3)