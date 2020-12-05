module Reader.Api

open Cache
open DocumentStore
open Shared.Api

let readerApi
    (cache:ICache)
    (documentStore:IDocumentStore)
    : IReaderApi =
    { listVehicles = fun () -> documentStore.ListVehicles()
      getVehicleCount = fun () -> cache.GetVehicleCount() }
