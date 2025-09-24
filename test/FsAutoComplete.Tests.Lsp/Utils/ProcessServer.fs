module Utils.ProcessServer

open System
open FsAutoComplete.Lsp
open FsAutoComplete
open Helpers
open Utils.ProcessLspServer
open FsAutoComplete.LspHelpers

/// Helper function that matches the existing serverInitialize signature but uses isolated server instances
let processServerInitialize path (config: FSharpConfigDto) =
    async {
        let fsAutoCompletePath = findFsAutoCompleteExecutable()
        let createServer = createProcessBasedServerFactory fsAutoCompletePath
        return! serverInitialize path config createServer
    }