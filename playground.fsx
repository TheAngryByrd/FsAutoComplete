open System.Threading
open System

#r "nuget: FSharp.Data.Adaptive"
open FSharp.Data.Adaptive


let input1 = cval 1
let _ = input1.AddWeakMarkingCallback(fun () -> printfn "input1 out of date")
// let _ = input1.AddWeakCallback(fun data -> printfn $"input1 value changed : {data}")

let input2 = cval 2
let _ = input2.AddWeakMarkingCallback(fun () -> printfn "input2 out of date")
// let _ = input2.AddWeakCallback(fun data -> printfn $"input2 value changed : {data}")

let derived12 = aval {
  let! i1 = input1
  and! i2 = input2
  return i1 + i2
}

let _ = derived12.AddWeakMarkingCallback(fun () -> printfn "derived12 out of date")
// let _ = derived12.AddWeakCallback(fun data -> printfn $"derived12 value changed : {data}")

let input3 = cval 3
let _ = input3.AddWeakMarkingCallback(fun () -> printfn "input3 out of date")
// let _ = input3.AddWeakCallback(fun data -> printfn $"input3 value changed : {data}")

let derived3 = aval {
  let! d1 = derived12
  and! i3 = input3
  return d1 * i3
}

let _ = derived3.AddWeakMarkingCallback(fun () -> printfn "derived3 out of date")
// let _ = derived3.AddWeakCallback(fun data -> printfn $"derived3 value changed : {data}")

transact(fun () ->
  input1.Value <- 7
)

derived3 |> AVal.force

// type SemaphoreSlim with

//   /// <summary>
//   /// Allows a semaphore to release with the IDisposable pattern
//   /// </summary>
//   /// <remarks>
//   /// Solves an issue where using the pattern:
//   /// <code>
//   /// try { await sem.WaitAsync(cancellationToken); }
//   /// finally { sem.Release(); }
//   /// </code>
//   /// Can result in SemaphoreFullException if the token is cancelled and the
//   /// the semaphore is incremented.
//   /// </remarks>
//   member semaphore.WaitDisposable(cancellationToken : CancellationToken) = task {
//     let t = semaphore.WaitAsync(cancellationToken)
//     do! t
//     return
//       {
//         new IDisposable with
//             member this.Dispose(): unit =
//                 if t.IsCompletedSuccessfully then semaphore.Release() |> ignore
//       }
//   }



let x1 = ["a", 1; "b", 2]
let x2 = ["a", 3, "c", 4]

let unionWith onConflict map1 map2 = 
  
  Map.fold (fun acc key value ->
    match Map.tryFind key acc with
    | Some l -> onConflict key l value |> fun newValue -> Map.add key newValue acc
    | None ->  Map.add key value acc
    ) map1 map2