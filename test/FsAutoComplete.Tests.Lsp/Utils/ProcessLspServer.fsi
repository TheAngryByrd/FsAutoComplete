module Utils.ProcessLspServer

open System
open FsAutoComplete.Lsp

/// Helper function to find the fsautocomplete executable
val findFsAutoCompleteExecutable : unit -> string

/// Create a factory that uses dotnet run to start FsAutoComplete as a separate process
/// This is a temporary approach to enable out-of-process testing
val createProcessBasedServerFactory : fsAutoCompletePath: string -> (unit -> IFSharpLspServer * System.IObservable<string * obj>)