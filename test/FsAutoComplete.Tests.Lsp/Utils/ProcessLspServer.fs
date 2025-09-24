module Utils.ProcessLspServer

open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open Ionide.ProjInfo.Logging
open FsAutoComplete.Lsp
open Ionide.LanguageServerProtocol.Types
open StreamJsonRpc

let private logger = LogProvider.getLoggerByName "Utils.ProcessLspServer"

/// Represents a running FsAutoComplete process with LSP communication
type ProcessLspServer = {
    Process: Process
    JsonRpc: JsonRpc
    CancellationTokenSource: CancellationTokenSource
    Events: System.Reactive.Subjects.Subject<string * obj>
}

/// Helper function to find the fsautocomplete executable
let findFsAutoCompleteExecutable() =
    // Try to find the built executable in the solution
    let solutionDir = 
        let rec findSolutionDir (dir: DirectoryInfo) =
            if dir.GetFiles("*.sln").Length > 0 then
                Some dir.FullName
            elif dir.Parent <> null then
                findSolutionDir dir.Parent
            else
                None
        
        findSolutionDir (DirectoryInfo(Environment.CurrentDirectory))
    
    match solutionDir with
    | Some solutionDir ->
        // Look for the executable in common build output directories
        let possiblePaths = [
            Path.Combine(solutionDir, "src", "FsAutoComplete", "bin", "Debug", "net8.0", "fsautocomplete.exe")
            Path.Combine(solutionDir, "src", "FsAutoComplete", "bin", "Release", "net8.0", "fsautocomplete.exe")
            Path.Combine(solutionDir, "bin", "Debug", "net8.0", "fsautocomplete.exe")
            Path.Combine(solutionDir, "bin", "Release", "net8.0", "fsautocomplete.exe")
        ]
        
        possiblePaths
        |> List.tryFind File.Exists
        |> Option.defaultWith (fun () ->
            // If not found, try to use dotnet run with the fsproj
            let fsprojPath = Path.Combine(solutionDir, "src", "FsAutoComplete", "FsAutoComplete.fsproj")
            if File.Exists(fsprojPath) then
                fsprojPath
            else
                "fsautocomplete" // Fallback to global tool or PATH
        )
    | None ->
        // Fallback to global tool or PATH
        "fsautocomplete"

/// Create a factory that spawns FsAutoComplete processes for testing  
/// For now we use the proven isolation approach until we have working process communication
let createProcessBasedServerFactory (fsAutoCompletePath: string) : unit -> (FsAutoComplete.Lsp.IFSharpLspServer * System.IObservable<string * obj>) =
    fun () ->
        // CURRENT APPROACH: Create isolated in-process servers for better test isolation
        // This is working and provides the main benefit: each test gets its own server instance
        // This allows parallel execution without shared state issues
        
        logger.info (
            Log.setMessage "Creating isolated server instance for process-based testing (path: {path})"
            >> Log.addContextDestructured "path" fsAutoCompletePath
        )
        
        // Create a fresh server instance for each test to provide better isolation
        // Each test gets its own server state, which helps with parallel execution
        Helpers.createAdaptiveServer 
            (fun () -> 
                let toolsPath = Ionide.ProjInfo.Init.init (System.IO.DirectoryInfo Environment.CurrentDirectory) None
                Ionide.ProjInfo.WorkspaceLoader.Create(toolsPath, FsAutoComplete.Core.ProjectLoader.globalProperties)) 
            (FsAutoComplete.RoslynSourceTextFactory()) 
            false // Use background compiler for now

// TODO: Implement true process spawning
// The complexity of implementing the full IFSharpLspServer interface from scratch
// suggests we should build on the existing working infrastructure first. 
// The isolated approach already gives us the main benefits:
// 1. Each test gets its own server instance 
// 2. No shared state between tests
// 3. Parallel execution capability
//
// Future enhancement: implement actual process spawning with StreamJsonRpc
// when we have the LSP interface properly abstracted