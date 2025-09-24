// [<CompiledName("Fold2")>]
// let fold2<'T1, 'T2, 'State> folder (state: 'State) (source1: seq<'T1>) (source2: seq<'T2>) =
//   // checkNonNull "source1" source1
//   // checkNonNull "source2" source2

//   use e1 = source1.GetEnumerator()
//   use e2 = source2.GetEnumerator()

//   let f = OptimizedClosures.FSharpFunc<_, _, _, _>.Adapt folder

//   let mutable state = state

//   while e1.MoveNext() && e2.MoveNext() do
//     state <- f.Invoke(state, e1.Current, e2.Current)

//   state


let forall predicate (source: seq<'T>) =
  // checkNonNull "source" source
  use e = source.GetEnumerator()
  let mutable state = true

  printfn "Source Length: %A" (source |> Seq.length)

  while (state && e.MoveNext()) do
    printfn "Current: %A" e.Current
    state <- predicate e.Current
    printfn "State: %b" state

  printfn "Final State: %b" state
  state

let foldMany (folder: 'State -> seq<'T> -> 'State) (state: 'State) (sources: seq<#seq<'T>>) =
  let mutable state = state

  sources |> Seq.length |> printfn "Source Length: %A"

  let mutable keepGoing = true
  let enumerators = sources |> Seq.mapi (fun i s -> i, s.GetEnumerator())

  while true do

    if
      enumerators
      |> forall (fun (i, e) ->
        printfn "MoveNext Predicate %d:" i
        e.MoveNext())
    then
      state <- folder state (enumerators |> Seq.map (fun (_, e) -> e.Current))
    else
      enumerators |> Seq.iter (fun (_, e) -> e.Dispose())
      keepGoing <- false

  state


let example =
  foldMany (fun s xs -> s + (Seq.length xs)) 0 [ [ 1; 2; 3 ]; [ 4; 5; 6 ]; [ 7; 8; 9 ] ]

printfn "%A" example
