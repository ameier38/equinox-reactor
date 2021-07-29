module Reactor.Config

open System

type HubConfig =
    { Url: string }
    static member Load() =
        let endpoint = Shared.Hub.Endpoints.Root
        { Url = Env.getVariable "HUB_URL" $"http://localhost:5000{endpoint}" }

type Config =
    { Debug: bool
      HubConfig: HubConfig
      CosmosDBConfig: Infrastructure.Config.CosmosDBConfig }
    static member Load() =
        { Debug = Env.getVariable "DEBUG" "false" |> Boolean.Parse
          HubConfig = HubConfig.Load()
          CosmosDBConfig = Infrastructure.Config.CosmosDBConfig.Load() }
