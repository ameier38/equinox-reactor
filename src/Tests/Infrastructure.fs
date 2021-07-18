[<AutoOpen>]
module Tests.Infrastructure

open System

module Env =

    let getEnv (key:string) (defaultValue:string) =
        match Environment.GetEnvironmentVariable(key) with
        | s when String.IsNullOrEmpty(s) -> defaultValue
        | s -> s
