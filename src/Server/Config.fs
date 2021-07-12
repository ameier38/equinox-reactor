module Server.Config

[<RequireQualifiedAccess>]
type AppEnv =
    | Prod
    | Dev

type ServerConfig =
    { Url: string }
    static member Load() =
        let host = Env.getVariable "SERVER_HOST" "0.0.0.0"
        let port = Env.getVariable "SERVER_PORT" "5000"
        { Url = $"http://{host}:{port}" }

type CosmosDBConfig =
    { Connection: string
      Database: string
      StoreContainer: string
      LeaseContainer: string }
    static member Load() =
        let storeContainer = Env.getVariable "COSMOSDB_CONTAINER" "test"
        { Connection = Env.getVariable "COSMOSDB_CONNECTION" "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=test;"
          Database = Env.getVariable "COSMOSDB_DATABASE" "test"
          StoreContainer = storeContainer
          LeaseContainer = $"{storeContainer}-aux" }

type SeqConfig =
    { Url: string }
    static member Load() =
        let scheme = Env.getVariable "SEQ_SCHEME" "http"
        let host = Env.getVariable "SEQ_HOST" "localhost"
        let port = Env.getVariable "SEQ_PORT" "5341" |> int
        { Url = $"{scheme}://{host}:{port}" }
        
type Config =
    { AppName: string
      AppEnv: AppEnv
      ServerConfig: ServerConfig
      CosmosDBConfig: CosmosDBConfig
      SeqConfig: SeqConfig }
    static member Load() =
        { AppName = Env.getVariable "APP_NAME" "EquinoxReactor"
          AppEnv = match Env.getVariable "APP_ENV" "dev" with "prod" -> AppEnv.Prod | _ -> AppEnv.Dev
          ServerConfig = ServerConfig.Load()
          CosmosDBConfig = CosmosDBConfig.Load()
          SeqConfig = SeqConfig.Load() }
