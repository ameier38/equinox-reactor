[<AutoOpen>]
module Client.Infrastructure

open Fable.Core

type Deferred<'T> =
    | HasNotStarted
    | InProgress
    | Resolved of 'T
    | Failed of string

[<RequireQualifiedAccess>]
module Env =
    [<Emit("import.meta.env[$0] ? import.meta.env[$0] : $1")>]
    let getEnv (key:string) (defaultValue:string): string = jsNative

[<RequireQualifiedAccess>]
module Log =
    let info (msg:obj) =
        JS.console.info(msg)

    let debug (msg:obj) =
        #if DEBUG
        JS.console.info(msg)
        #else
        ()
        #endif

    let error (error:obj) =
        #if DEBUG
        JS.console.error(error)
        #else
        ()
        #endif
