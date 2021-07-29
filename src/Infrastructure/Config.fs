module Infrastructure.Config

type CosmosDBConfig =
    { Connection: string
      Database: string
      StoreContainer: string
      LeaseContainer: string }
    static member Load() =
        let storeContainer = Env.getVariable "COSMOSDB_CONTAINER" "test"
        { Connection = Env.getSecret "cosmosdb" "connection" "COSMOSDB_CONNECTION" "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=test;"
          Database = Env.getVariable "COSMOSDB_DATABASE" "test"
          StoreContainer = storeContainer
          LeaseContainer = $"{storeContainer}-aux" }

