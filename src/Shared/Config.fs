namespace Shared

type StreamConfig =
    { VehicleCategory: string }
    static member Load() =
        { VehicleCategory = "Vehicle" }

type ServerConfig =
    { Host: string
      Port: int
      ClientUrl: string }
    static member Load() =
        let clientScheme = Env.getEnv "CLIENT_SCHEME" "http"
        let clientHost = Env.getEnv "CLIENT_HOST" "localhost"
        let clientPort = Env.getEnv "CLIENT_PORT" "3000"
        { Host = Env.getEnv "SERVER_HOST" "0.0.0.0"
          Port = Env.getEnv "SERVER_PORT" "5000" |> int
          ClientUrl = sprintf "%s://%s:%s" clientScheme clientHost clientPort }

type EventStoreDBConfig =
    { Url: string
      User: string
      Password: string }
    static member Load() =
        let scheme = Env.getEnv "EVENTSTORE_SCHEME" "tcp"
        let host = Env.getEnv "EVENTSTORE_HOST" "localhost"
        let port = Env.getEnv "EVENTSTORE_PORT" "1113" |> int
        let user = Env.getEnv "EVENTSTORE_USER" "admin"
        let password = Env.getEnv "EVENTSTORE_PASSWORD" "changeit"
        { Url = sprintf "%s://%s:%i" scheme host port
          User = user
          Password = password }

type RedisConfig =
    { ConnStr: string
      VehicleCountKey: string }
    static member Load() =
        let host = Env.getEnv "REDIS_HOST" "localhost"
        let port = Env.getEnv "REDIS_PORT" "6379" |> int
        let password = Env.getEnv "REDIS_PASSWORD" "changeit"
        { ConnStr = sprintf "%s:%i,password=%s" host port password
          VehicleCountKey = "vehicleCount" }

type LiteDBConfig =
    { Filename: string
      VehicleOverviewCollection: string }
    static member Load() =
        { Filename = Env.getEnv "LITEDB_FILENAME" "dealership.db"
          VehicleOverviewCollection = "vehicles" }

type SeqConfig =
    { Url: string }
    static member Load() =
        let scheme = Env.getEnv "SEQ_SCHEME" "http"
        let host = Env.getEnv "SEQ_HOST" "localhost"
        let port = Env.getEnv "SEQ_PORT" "5341" |> int
        { Url = sprintf "%s://%s:%i" scheme host port }
