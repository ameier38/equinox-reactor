[<AutoOpen>]
module Domain.Infrastructure

module Log =
    let forMetrics () =
        Serilog.Log.ForContext("isMetric", true)

module Equinox =
    let createDecider stream =
        Equinox.Decider(Log.forMetrics (), stream, maxAttempts = 3)
