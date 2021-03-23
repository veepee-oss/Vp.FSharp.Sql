module internal Vp.FSharp.Sql.Helpers

open System
open System.Data
open System.Threading
open System.Data.Common
open System.Transactions
open System.Threading.Tasks
open System.Text.RegularExpressions

open FSharp.Control


[<RequireQualifiedAccess>]
module DbConnection  =

    let enlistCurrentTransaction (connection: #DbConnection) =
        connection.EnlistTransaction(Transaction.Current)

    let isClosed (connection: #DbConnection) =
        connection.State = ConnectionState.Closed

    let openIfClosed cancellationToken closed (connection: #DbConnection) =
        async { if closed then do! connection.OpenAsync(cancellationToken) |> Async.AwaitTask }

    let openIfClosedSync closed (connection: #DbConnection) =
        if closed then connection.Open()

    let closedIfClosed closed (connection: #DbConnection) =
        if closed then connection.Close()

type DbDataReader with

    member this.AwaitRead(cancellationToken) = this.ReadAsync(cancellationToken) |> Async.AwaitTask
    member this.AwaitNextResult(cancellationToken) = this.NextResultAsync(cancellationToken) |> Async.AwaitTask
    member this.AwaitTryReadNextResult(cancellationToken) =
        async {
            let! nextResultOk = this.AwaitNextResult(cancellationToken)
            if nextResultOk then return! this.AwaitRead(cancellationToken)
            else return nextResultOk
        }
    member this.TryReadNextResult() =
        let nextResultOk = this.NextResult()
        if nextResultOk then this.Read()
        else nextResultOk

[<RequireQualifiedAccess>]
module String =

    [<Literal>]
    let ConnectionStringSeparator = ";"

    [<Literal>]
    let EmptyStringConstant = ""

    [<Literal>]
    let SqlNewLineConstant = "\n"

    let private trimLeftPattern = Regex(sprintf "%s[ ]+" SqlNewLineConstant, RegexOptions.Compiled)

    let trimLeft str = trimLeftPattern.Replace(str, SqlNewLineConstant)

    let stitch strs = String.concat SqlNewLineConstant strs

let def<'T> = Unchecked.defaultof<'T>

[<AbstractClass; Sealed>]
type Async private () =
    static member AwaitValueTask(valueTask: ValueTask) = valueTask.AsTask() |> Async.AwaitTask
    static member AwaitValueTask(valueTask: ValueTask<'T>) = valueTask.AsTask() |> Async.AwaitTask


[<RequireQualifiedAccess>]
module Async =
    let linkedTokenSourceFrom cancellationToken =
        async {
            let! token = Async.CancellationToken
            use mergedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken)
            return mergedTokenSource.Token
        }


[<RequireQualifiedAccess>]
module SkipFirstAsyncSeq =

    let scan folder state source =
        AsyncSeq.scan folder state source
        |> AsyncSeq.skip(1)

    let scanAsync folder state source =
        AsyncSeq.scanAsync folder state source
        |> AsyncSeq.skip(1)

[<RequireQualifiedAccess>]
module SkipFirstSeq =
    let scan folder state source =
        Seq.scan folder state source
        |> Seq.skip(1)

[<RequireQualifiedAccess>]
module AsyncSeq =

    let mapbi mapping source =
        source
        |> SkipFirstAsyncSeq.scan(fun state item -> (fst state + 1I, item)) (-1I, def)
        |> AsyncSeq.map(fun (bi, item) -> mapping bi item)

    let mapChange selector mapping source =
        source
        |> mapbi (fun bi item -> (bi, item))
        |> AsyncSeq.scan(fun (previousSelection, previousMappedItem, _) (bi, item) ->
            if bi = 0I then
                (selector item, mapping item, item)
            else
                let currentSelection = selector item
                let mappedItem = mapping item
                if previousSelection <> currentSelection then (currentSelection, mappedItem, item)
                else (previousSelection, previousMappedItem, item)
            ) (def, def, def)
        |> AsyncSeq.skip 1
        |> AsyncSeq.map(fun (_, mappedItem, item) -> (item, mappedItem))

    let consume source = AsyncSeq.iter(fun _ -> ()) source

[<RequireQualifiedAccess>]
module Seq =

    let mapbi mapping source =
        source
        |> SkipFirstSeq.scan(fun state item -> (fst state + 1I, item)) (-1I, def)
        |> Seq.map(fun (bi, item) -> mapping bi item)

    let mapChange selector mapping source =
        source
        |> mapbi (fun bi item -> (bi, item))
        |> Seq.scan(fun (previousSelection, previousMappedItem, _) (bi, item) ->
            if bi = 0I then
                (selector item, mapping item, item)
            else
                let currentSelection = selector item
                let mappedItem = mapping item
                if previousSelection <> currentSelection then (currentSelection, mappedItem, item)
                else (previousSelection, previousMappedItem, item)
            ) (def, def, def)
        |> Seq.skip 1
        |> Seq.map(fun (_, mappedItem, item) -> (item, mappedItem))

    let consume source = Seq.iter(fun _ -> ()) source

module DbNull =
    let is<'T>() = typedefof<'T> = typedefof<DBNull>

    let retypedAs<'T>() = DBNull.Value :> obj :?> 'T
