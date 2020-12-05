module Reactor.Cache

open Serilog
open Shared
open StackExchange.Redis

type ICache =
    abstract GetVehicleCountCheckpoint: unit -> Async<int64>
    abstract IncrementVehicleCount: checkpoint:int64 -> Async<unit>
    abstract DecrementVehicleCount: checkpoint:int64 -> Async<unit>

type RedisCache(config: RedisConfig) =
    let log = Log.ForContext<RedisCache>()
    let conn = ConnectionMultiplexer.Connect(config.ConnStr)
    let db = conn.GetDatabase()

    let getCheckpointAsync (key: string) =
        async {
            log.Debug("Getting checkpoint for {key}", key)
            let redisKey = RedisKey(sprintf "checkpoint:%s" key)
            let! redisValue = db.StringGetAsync(redisKey) |> Async.AwaitTask
            let (value: string) = if redisValue.HasValue then RedisValue.op_Implicit redisValue else "0"
            log.Debug("Got checkpoint {Checkpoint} for {Key}", value, config.VehicleCountKey)
            return int64 value
        }

    let updateCheckpoint (tran: ITransaction, key: string, value: int64) =
        log.Debug("Updating checkpoint for {Key} to {Value}", key, value)
        let redisKey = RedisKey(sprintf "checkpoint:%s" key)
        let redisValue = RedisValue.op_Implicit value
        // NB: we ignore the task result since it will not get executed until tran.ExecuteAsync is called
        tran.StringSetAsync(redisKey, redisValue)
        |> ignore

    let increment (tran: ITransaction, key: string) =
        log.Debug("Incrementing {Key}", key)
        let redisKey = RedisKey(key)
        // NB: we ignore the task result since it will not get executed until tran.ExecuteAsync is called
        tran.StringIncrementAsync(redisKey) |> ignore

    let decrement (tran: ITransaction, key: string) =
        log.Debug("Decrementing {Key}", key)
        let redisKey = RedisKey(key)
        // NB: we ignore the task result since it will not get executed until tran.ExecuteAsync is called
        tran.StringDecrementAsync(redisKey) |> ignore

    let transactAsync (work: ITransaction -> unit) =
        async {
            let tran = db.CreateTransaction()
            do work tran
            let! completed = tran.ExecuteAsync() |> Async.AwaitTask
            if not completed then failwithf "Transaction failed"
        }

    interface ICache with
        member _.GetVehicleCountCheckpoint() =
            getCheckpointAsync (config.VehicleCountKey)

        member _.IncrementVehicleCount(checkpoint: int64) =
            let work (tran: ITransaction) =
                increment (tran, config.VehicleCountKey)
                updateCheckpoint (tran, config.VehicleCountKey, checkpoint)

            transactAsync work

        member _.DecrementVehicleCount(checkpoint: int64) =
            let work (tran: ITransaction) =
                decrement (tran, config.VehicleCountKey)
                updateCheckpoint (tran, config.VehicleCountKey, checkpoint)

            transactAsync work
