if
  System.Environment.GetCommandLineArgs()
  |> Array.tryItem 1
  |> Option.exists (fun i -> i.EndsWith __SOURCE_FILE__)
then
  printfn "Hello from playground4.fsx"
