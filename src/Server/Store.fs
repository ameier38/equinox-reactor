﻿module Server.Store

open Server.Config
open System

module Cosmos =
    open Equinox.CosmosStore

    type CosmosStore(config:CosmosDBConfig) =
        let discovery = Discovery.ConnectionString config.Connection
        let timeout = TimeSpan.FromSeconds 5.
        let maxRetries = 3
        let maxRetryTimeout = TimeSpan.FromSeconds 5.
        let connector = CosmosStoreConnector(discovery, timeout, maxRetries, maxRetryTimeout)
        let storeContainer =
            connector
                .CreateUninitialized()
                .GetDatabase(config.Database)
                .GetContainer(config.StoreContainer)
        let leaseContainer =
            connector
                .CreateUninitialized()
                .GetDatabase(config.Database)
                .GetContainer(config.LeaseContainer)
        let storeClient = CosmosStoreClient(storeContainer.Database.Client, config.Database, config.StoreContainer)
        
        member _.Context = CosmosStoreContext(storeClient, tipMaxEvents=50)
        member _.StoreContainer = storeContainer
        member _.LeaseContainer = leaseContainer
