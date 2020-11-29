#load ".fake/build.fsx/intellisense.fsx"
open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
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

let _default = BuildTask.createEmpty "Default" []

BuildTask.runOrDefault _default