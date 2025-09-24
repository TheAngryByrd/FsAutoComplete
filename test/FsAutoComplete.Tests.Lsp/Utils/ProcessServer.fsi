module Utils.ProcessServer

open FsAutoComplete.Lsp
open FsAutoComplete.LspHelpers

/// Helper function that matches the existing serverInitialize signature but uses isolated server instances
val processServerInitialize : path: string -> config: FSharpConfigDto -> Async<IFSharpLspServer * System.IObservable<string * obj>>