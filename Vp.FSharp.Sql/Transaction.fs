[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.Transaction

open System.Data
open System.Threading
open System.Data.Common

open Vp.FSharp.Sql.Helpers


[<Literal>]
let DefaultIsolationLevel = IsolationLevel.ReadCommitted

let commit cancellationToken isolationLevel (connection: #DbConnection) body =
    async {
        let wasClosed = connection.State = ConnectionState.Closed
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! connection.OpenAsync(linkedToken) |> Async.AwaitTask
            use! transaction = connection.BeginTransactionAsync(isolationLevel, linkedToken) |> Async.AwaitValueTask
            let! actionResult = body connection
            do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
            return actionResult
        finally
            if wasClosed then connection.Close()
    }

let notCommit cancellationToken isolationLevel (connection: #DbConnection) body =
    async {
        let wasClosed = connection.State = ConnectionState.Closed
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! connection.OpenAsync(linkedToken) |> Async.AwaitTask
            use! _transaction = connection.BeginTransactionAsync(isolationLevel, linkedToken) |> Async.AwaitValueTask
            let! actionResult = body connection
            return actionResult
        finally
            if wasClosed then connection.Close()
    }

let commitOnSome cancellationToken isolationLevel (connection: #DbConnection) body =
    async {
        let wasClosed = connection.State = ConnectionState.Closed
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! connection.OpenAsync(linkedToken) |> Async.AwaitTask
            use! transaction = connection.BeginTransactionAsync(isolationLevel, linkedToken) |> Async.AwaitValueTask
            match! body connection with
            | Some some ->
                do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
                return Some some
            | None ->
                return None
        finally
            if wasClosed then connection.Close()
    }

let commitOnOk cancellationToken isolationLevel (connection: #DbConnection) body =
    async {
        let wasClosed = connection.State = ConnectionState.Closed
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! connection.OpenAsync(linkedToken) |> Async.AwaitTask
            use! transaction = connection.BeginTransactionAsync(isolationLevel, linkedToken) |> Async.AwaitValueTask
            match! body connection with
            | Ok ok ->
                do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
                return Ok ok
            | Error error ->
                return Error error
        finally
            if wasClosed then connection.Close()
    }

let defaultCommit connection body =
    commit  CancellationToken.None DefaultIsolationLevel connection body

let defaultNotCommit connection body =
    notCommit CancellationToken.None DefaultIsolationLevel connection body

let defaultCommitOnSome connection body =
    commitOnSome CancellationToken.None DefaultIsolationLevel connection body

let defaultCommitOnOk connection body =
    commitOnOk CancellationToken.None DefaultIsolationLevel connection body
