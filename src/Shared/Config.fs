namespace Shared

type StreamConfig =
    { VehicleCategory: string }
    static member Load() =
        { VehicleCategory = "Vehicle" }

type CollectionConfig =
    { CheckpointsCollection: string
      VehiclesCollection: string }
    static member Load() =
        { CheckpointsCollection = "checkpoints"
          VehiclesCollection = "vehicles" }

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

type LiteDBConfig =
    { DataPath: string }
    static member Load() =
        { DataPath = Env.getEnv "LITEDB_DATA_PATH" "dealership.db" }