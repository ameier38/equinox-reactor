#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.JavaScript
open BlackFox.Fake

let clean = BuildTask.create "Clean" [] {
    !! "src/**/bin"
    ++ "src/**/obj"
    |> Shell.cleanDirs 
}

BuildTask.create "Restore" [clean.IfNeeded] {
    !! "src/**/*.*proj"
    |> Seq.iter (DotNet.restore id)
}

let run (proj:string) = 
    let result = DotNet.exec id "run" (sprintf "--project src/%s/%s.fsproj" proj proj)
    if not result.OK then failwithf "Error! %A" result.Errors

BuildTask.create "StartProcessor" [] {
    run "Processor"
}

BuildTask.create "StartReactor" [] {
    run "Reactor"
}

BuildTask.create "StartReader" [] {
    run "Reader"
}

BuildTask.create "StartClient" [] {
    Npm.run "start" id
}

BuildTask.create "TestUnits" [] {
    run "UnitTests"
}

BuildTask.create "TestIntegrations" [] {
    run "IntegrationTests"
}

let publish (proj:string) =
    Trace.tracef "Publishing %s..." proj
    // ref: https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
    let runtime =
        if Environment.isLinux then "linux-x64"
        elif Environment.isWindows then "win-x64"
        elif Environment.isMacOS then "osx-x64"
        else failwithf "environment not supported"
    DotNet.publish (fun args ->
        { args with
            OutputPath = Some (sprintf "src/%s/out" proj)
            Runtime = Some runtime })
        (sprintf "src/%s/%s.fsproj" proj proj)

BuildTask.create "PublishProcessor" [] {
    publish "Processor"
}

BuildTask.create "PublishReactor" [] {
    publish "Reactor"
}

BuildTask.create "PublishReader" [] {
    publish "Reader"
}

BuildTask.create "PublishIntegrationTests" [] {
    publish "IntegrationTests"
}

BuildTask.create "BuildClient" [] {
    Npm.run "build" id
}

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default