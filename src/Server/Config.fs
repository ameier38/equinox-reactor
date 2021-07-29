module Server.Config

type ServerConfig =
    { Url: string }
    static member Load() =
        let host = Env.getVariable "SERVER_HOST" "0.0.0.0"
        let port = Env.getVariable "SERVER_PORT" "5000"
        { Url = $"http://{host}:{port}" }

type SeqConfig =
    { Url: string }
    static member Load() =
        let scheme = Env.getVariable "SEQ_SCHEME" "http"
        let host = Env.getVariable "SEQ_HOST" "localhost"
        let port = Env.getVariable "SEQ_PORT" "5341" |> int
        { Url = $"{scheme}://{host}:{port}" }
        
type Config =
    { AppName: string
      ServerConfig: ServerConfig
      CosmosDBConfig: Infrastructure.Config.CosmosDBConfig
      SeqConfig: SeqConfig }
    static member Load() =
        { AppName = Env.getVariable "APP_NAME" "EquinoxReactor"
          ServerConfig = ServerConfig.Load()
          CosmosDBConfig = Infrastructure.Config.CosmosDBConfig.Load()
          SeqConfig = SeqConfig.Load() }
