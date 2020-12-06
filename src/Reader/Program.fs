open Fable.Remoting.Server
open Fable.Remoting.Suave
open Reader
open Serilog
open Shared
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.Writers

let setCORsHeaders (clientUrl:string) =
    setHeader "Access-Control-Allow-Origin" clientUrl
    >=> setHeader "Access-Control-Allow-Methods" "GET,POST,OPTIONS"
    >=> setHeader "Access-Control-Allow-Credentials" "true"
    >=> setHeader "Access-Control-Allow-Headers" "content-type,x-remoting-proxy"

[<EntryPoint>]
let main _ =
    let debug = Env.getEnv "DEBUG" "false" |> bool.Parse
    let seqConfig = SeqConfig.Load()
    let serverConfig = ServerConfig.Load()
    let liteDBConfig = LiteDBConfig.Load()
    let redisConfig = RedisConfig.Load()
    let logger =
        LoggerConfiguration()
            .MinimumLevel.Is(if debug then Events.LogEventLevel.Debug else Events.LogEventLevel.Information)
            .Enrich.WithProperty("Application", "Processor")
            .WriteTo.Console()
            .WriteTo.Seq(seqConfig.Url)
            .CreateLogger()
    Log.Logger <- logger
    let store = DocumentStore.LiteDBDocumentStore(liteDBConfig)
    let cache = Cache.RedisCache(redisConfig)
    let api: WebPart =
        Remoting.createApi()
        |> Remoting.fromValue (Api.readerApi cache store)
        |> Remoting.buildWebPart
    let app: WebPart =
        choose [
            path "/heathz" >=> OK "Healthy!"
            OPTIONS >=> setCORsHeaders serverConfig.ClientUrl >=> OK "CORS allowed"
            setCORsHeaders serverConfig.ClientUrl >=> api
        ]
    let bindings = [HttpBinding.createSimple HTTP serverConfig.Host serverConfig.Port ]
    let suaveConfig = { defaultConfig with bindings = bindings }
    startWebServer suaveConfig app
    Log.CloseAndFlush()
    0 // return an integer exit code
