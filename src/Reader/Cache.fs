module Reader.Cache

open Shared
open StackExchange.Redis

type ICache =
    abstract member GetVehicleCount: unit -> Async<int>

type RedisCache(config:RedisConfig) =
    let conn = ConnectionMultiplexer.Connect(config.ConnStr)
    let db = conn.GetDatabase()

    let getVehicleCount () =
        async {
            let redisKey = RedisKey(config.VehicleCountKey)
            let! redisValue = db.StringGetAsync(redisKey) |> Async.AwaitTask
            let (value:string) = if redisValue.HasValue then RedisValue.op_Implicit redisValue else "0"
            return int value
        }

    interface ICache with
        member _.GetVehicleCount() = getVehicleCount()
