namespace FsAutoComplete


open System.Threading
open System.Threading.Tasks
open FSharp.Data.Adaptive
open System
open IcedTasks
type internal RefCountingTaskCreator<'a>(create : CancellationToken -> Task<'a>) =

    let mutable refCount = 0
    let mutable cache : option<Task<'a>> = None
    let mutable cancel : CancellationTokenSource = null

    member private x.RemoveRef() =
        lock x (fun () ->
            if refCount = 1 then
                refCount <- 0
                cancel.Cancel()
                cancel.Dispose()
                cancel <- null
                cache <- None
            else
                refCount <- refCount - 1
        )

    member x.New() =
        lock x (fun () ->
            match cache with
            | Some cache ->
                refCount <- refCount + 1
                CancelableTask(x.RemoveRef, cache)
            | None ->
                cancel <- new CancellationTokenSource()
                let task = create cancel.Token
                cache <- Some task
                refCount <- refCount + 1
                CancelableTask(x.RemoveRef, task)
        )


and [<Struct>] CancelableTask<'a>(cancel : unit -> unit, real : Task<'a>) =

    member x.GetAwaiter() = real.GetAwaiter()
    member x.Cancel() =
        cancel()
    member x.Task =
        real

type asyncaval<'a> =
    inherit IAdaptiveObject
    abstract GetValue : AdaptiveToken -> CancelableTask<'a>

module AsyncAVal =

    let force (value : asyncaval<_>) = value.GetValue(AdaptiveToken.Top)


    let forceAsync (value : asyncaval<_>) =
      async {
        let! ctok = Async.CancellationToken
        let ct = value.GetValue(AdaptiveToken.Top)
        use _ = ctok.Register(fun () -> ct.Cancel())
        return! ct.Task |> Async.AwaitTask
      }

    type ConstantVal<'a>(value : CancelableTask<'a>) =
        inherit ConstantObject()

        new (value : Task<'a>) = ConstantVal<'a>(CancelableTask(id, value))

        interface asyncaval<'a> with
            member x.GetValue _ = value

    [<AbstractClass>]
    type AbstractVal<'a>() =
        inherit AdaptiveObject()
        abstract Compute : AdaptiveToken -> CancelableTask<'a>

        member x.GetValue token =
            x.EvaluateAlways token x.Compute

        interface asyncaval<'a> with
            member x.GetValue t = x.GetValue t

    let constant (value : 'a) =
        ConstantVal(Task.FromResult value) :> asyncaval<_>

    let ofTask (value : Task<'a>) =
        ConstantVal(value) :> asyncaval<_>

    let ofCancelableTask (value : CancelableTask<'a>) =
        ConstantVal(value.Task) :> asyncaval<_>

    // let ofCancellableTask (value : CancellableTask<'a>) =
    //     ConstantVal (
    //         let cts = new CancellationTokenSource ()
    //         let cancel () =
    //             cts.Cancel()
    //             cts.Dispose()
    //         let real = task {
    //             try
    //                 return! value cts.Token
    //             finally
    //                 cts.Dispose()
    //         }
    //         CancelableTask(cancel, real)
    //     )
    //     :> asyncaval<_>

    let ofAVal (value : aval<'a>) =
        if value.IsConstant then
            ConstantVal(Task.FromResult (AVal.force value)) :> asyncaval<_>
        else
            { new AbstractVal<'a>() with
                member x.Compute t =
                    let real = Task.FromResult(value.GetValue t)
                    CancelableTask(id, real)
            } :> asyncaval<_>

    let map (mapping : 'a -> CancellationToken -> Task<'b>) (input : asyncaval<'a>) =
        let mutable cache : option<RefCountingTaskCreator<'b>> = None
        { new AbstractVal<'b>() with
            member x.Compute t =
                if x.OutOfDate || Option.isNone cache then
                    let ref =
                        RefCountingTaskCreator(cancellableTask {
                            let! ct = CancellableTask.getCancellationToken ()
                            let it = input.GetValue t
                            let s = ct.Register(fun () -> it.Cancel())
                            try
                                let! i = it
                                return! mapping i ct
                            finally
                                s.Dispose()
                            }
                        )
                    cache <- Some ref
                    ref.New()
                else
                    cache.Value.New()
        } :> asyncaval<_>


    let mapSync (mapping : 'a -> CancellationToken -> 'b) (input : asyncaval<'a>) =
        let mutable cache : option<RefCountingTaskCreator<'b>> = None
        { new AbstractVal<'b>() with
            member x.Compute t =
                if x.OutOfDate || Option.isNone cache then
                    let ref =
                        RefCountingTaskCreator(cancellableTask {
                            let! ct = CancellableTask.getCancellationToken ()
                            let it = input.GetValue t
                            let s = ct.Register(fun () -> it.Cancel())
                            try
                                let! i = it
                                return mapping i ct
                            finally
                                s.Dispose()
                            }
                        )
                    cache <- Some ref
                    ref.New()
                else
                    cache.Value.New()
        } :> asyncaval<_>

    let map2 (mapping : 'a -> 'b -> CancellationToken -> Task<'c>) (ca : asyncaval<'a>) (cb : asyncaval<'b>) =
        let mutable cache : option<RefCountingTaskCreator<'c>> = None
        { new AbstractVal<'c>() with
            member x.Compute t =
                if x.OutOfDate || Option.isNone cache then
                    let ref =
                        RefCountingTaskCreator(cancellableTask {
                            let ta = ca.GetValue t
                            let tb = cb.GetValue t

                            let! ct = CancellableTask.getCancellationToken ()
                            let s = ct.Register(fun () -> ta.Cancel(); tb.Cancel())

                            try
                                let! va = ta
                                let! vb = tb
                                return! mapping va vb ct
                            finally
                                s.Dispose()
                            }
                        )
                    cache <- Some ref
                    ref.New()
                else
                    cache.Value.New()
        } :> asyncaval<_>

    // untested!!!!
    let bind (mapping : 'a -> CancellationToken -> asyncaval<'b>) (value : asyncaval<'a>) =
        let mutable cache : option<_> = None
        let mutable innerCache : option<_> = None
        let mutable inputChanged = 0
        let inners : ref<HashSet<asyncaval<'b>>> = ref HashSet.empty

        { new AbstractVal<'b>() with

            override x.InputChangedObject(_, o) =
                if System.Object.ReferenceEquals(o, value) then
                    inputChanged <- 1
                    lock inners (fun () ->
                        for i in inners.Value do i.Outputs.Remove x |> ignore
                        inners.Value <- HashSet.empty
                    )

            member x.Compute t =
                if x.OutOfDate then
                    if Interlocked.Exchange(&inputChanged, 0) = 1 || Option.isNone cache then
                        let outerTask =
                            RefCountingTaskCreator(cancellableTask {
                                let it = value.GetValue t
                                let! ct = CancellableTask.getCancellationToken ()
                                let s = ct.Register(fun () -> it.Cancel())

                                try
                                    let! i = it
                                    let inner = mapping i ct
                                    return inner
                                finally
                                    s.Dispose()
                                }
                            )
                        cache <- Some outerTask

                    let outerTask = cache.Value
                    let ref =
                        RefCountingTaskCreator(cancellableTask {
                            let innerCellTask = outerTask.New()

                            let! ct = CancellableTask.getCancellationToken ()
                            let s = ct.Register(fun () -> innerCellTask.Cancel())

                            try
                                let! inner = innerCellTask
                                let innerTask = inner.GetValue t
                                lock inners (fun () -> inners.Value <- HashSet.add inner inners.Value)
                                let s2 =
                                    ct.Register(fun () ->
                                        innerTask.Cancel()
                                        lock inners (fun () -> inners.Value <- HashSet.remove inner inners.Value)
                                        inner.Outputs.Remove x |> ignore
                                    )
                                try
                                    let! innerValue = innerTask
                                    return innerValue
                                finally
                                    s2.Dispose()
                            finally
                                s.Dispose()
                            }
                        )

                    innerCache <- Some ref

                    ref.New()
                else
                    innerCache.Value.New()

        } :> asyncaval<_>




type AsyncAValBuilder () =

    member inline x.MergeSources(v1 : asyncaval<'T1>, v2 : asyncaval<'T2>) =
        AsyncAVal.map2 (fun a b _ -> Task.FromResult(a, b)
        ) v1 v2

    // member inline x.MergeSources3(v1 : aval<'T1>, v2 : aval<'T2>, v3 : aval<'T3>) =
    //     AVal.map3 (fun a b c -> a,b,c) v1 v2 v3

    member inline x.BindReturn(value : asyncaval<'T1>, mapping: 'T1 -> CancellationToken -> Task<'T2>) =
        AsyncAVal.map mapping value

    member inline x.BindReturn(value : asyncaval<'T1>, mapping: 'T1 -> Task<'T2>) =
        AsyncAVal.map (fun data _ -> mapping data) value

    // member inline x.Bind2Return(v1 : aval<'T1>, v2 : aval<'T2>, mapping: 'T1 * 'T2 -> 'T3) =
    //     AVal.map2 (fun a b -> mapping(a,b)) v1 v2

    // member inline x.Bind3Return(v1 : aval<'T1>, v2: aval<'T2>, v3: aval<'T3>, mapping: 'T1 * 'T2 * 'T3 -> 'T4) =
    //     AVal.map3 (fun a b c -> mapping(a, b, c)) v1 v2 v3

    member inline x.Bind(value: asyncaval<'T1>, mapping: 'T1 -> CancellationToken -> asyncaval<'T2>) =
        AsyncAVal.bind (mapping) value

    member inline x.Bind(value: asyncaval<'T1>, mapping: 'T1  -> asyncaval<'T2>) =
        AsyncAVal.bind (fun data _ -> mapping data) value

    // member inline x.Bind2(v1: aval<'T1>, v2: aval<'T2>, mapping: 'T1 * 'T2 -> aval<'T3>) =
    //     AVal.bind2 (fun a b -> mapping(a,b)) v1 v2

    // member inline x.Bind3(v1: aval<'T1>, v2: aval<'T2>, v3: aval<'T3>, mapping: 'T1 * 'T2 * 'T3 -> aval<'T4>) =
    //     AVal.bind3 (fun a b c -> mapping(a, b, c)) v1 v2 v3

    member inline x.Return(value: 'T) =
        AsyncAVal.constant value

    member inline x.ReturnFrom(value: asyncaval<'T>) =
        value

    member inline x.Source(value : asyncaval<'T>) = value
[<AutoOpen>]
module AsyncAValBuilderExtensions =
    let asyncAVal = AsyncAValBuilder()
    type AsyncAValBuilder with
        member inline x.Source(value : aval<'T>) = AsyncAVal.ofAVal value
        member inline x.Source(value : Task<'T>) = AsyncAVal.ofTask value
        member inline x.Source(value : CancelableTask<'T>) = AsyncAVal.ofCancelableTask value
        // member inline x.Source(value : CancellableTask<'T>) = AsyncAVal.ofCancellableTask value

        member inline x.BindReturn(value : asyncaval<'T1>, mapping: 'T1 -> 'T2) =
            AsyncAVal.map (fun data _ -> mapping data |> Task.FromResult) value
