module internal Vp.FSharp.Sql.Helpers

open System
open System.Data.Common
open System.Threading
open System.Text.RegularExpressions
open System.Transactions

open FSharp.Control


type DbConnection with

    member this.EnlistCurrentTransaction() = this.EnlistTransaction(Transaction.Current)


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


[<RequireQualifiedAccess>]
module Async =
    let linkedTokenSourceFrom cancellationToken =
        async {
            let! token = Async.CancellationToken
            use mergedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, cancellationToken)
            return mergedTokenSource.Token
        }

[<RequireQualifiedAccess>]
module AsyncSeq =

    let mapbi mapping source =
        source
        |> AsyncSeq.scan(fun state item -> (fst state + 1, item)) (-1, def)
        |> AsyncSeq.skip(1)
        |> AsyncSeq.map(fun (bi, item) -> mapping bi item)

    let mapChange selector mapping source =
        source
        |> mapbi (fun bi item -> (bi, item))
        |> AsyncSeq.scan(fun (previousSelection, previousMappedItem, _) (bi, item) ->
            if bi = 0 then
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

module DbNull =
    let is<'T>() = typedefof<'T> = typedefof<DBNull>

    let retypedAs<'T>() = DBNull.Value :> obj :?> 'T
