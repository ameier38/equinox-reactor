module Client.Api

open Fable.Remoting.Client
open Shared.Api

let processorConfig = Config.ProcessorConfig.Load()
let readerConfig = Config.ReaderConfig.Load()

let processorApi =
    Remoting.createApi()
    |> Remoting.withBaseUrl processorConfig.Url
    |> Remoting.buildProxy<IProcessorApi>

let readerApi =
    Remoting.createApi()
    |> Remoting.withBaseUrl readerConfig.Url
    |> Remoting.buildProxy<IReaderApi>
