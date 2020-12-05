module Client.Program

open Elmish
open Elmish.React

#if DEBUG
open Elmish.HMR
#endif

Program.mkProgram App.State.init App.State.update App.View.render
|> Program.withReactSynchronous "app"
|> Program.run
