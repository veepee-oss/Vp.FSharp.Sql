[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.Transaction

open System.Data
open System.Threading
open System.Data.Common

open Vp.FSharp.Sql.Helpers


[<Literal>]
let DefaultIsolationLevel = IsolationLevel.ReadCommitted

let commit (connection: #DbConnection) cancellationToken isolationLevel action =
    async {
        let wasClosed = connection.State = ConnectionState.Closed
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! connection.OpenAsync(linkedToken) |> Async.AwaitTask
            use! transaction = connection.BeginTransactionAsync(isolationLevel, linkedToken).AsTask() |> Async.AwaitTask
            let! actionResult = action()
            do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
            return actionResult
        finally
            if wasClosed then connection.Close()
    }

let notCommit (connection: #DbConnection) cancellationToken isolationLevel action =
    async {
        let wasClosed = connection.State = ConnectionState.Closed
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! connection.OpenAsync(linkedToken) |> Async.AwaitTask
            use! _transaction = connection.BeginTransactionAsync(isolationLevel, linkedToken).AsTask()
                               |> Async.AwaitTask
            let! actionResult = action()
            return actionResult
        finally
            if wasClosed then connection.Close()
    }

let commitOnSome (connection: #DbConnection) cancellationToken isolationLevel action =
    async {
        let wasClosed = connection.State = ConnectionState.Closed
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! connection.OpenAsync(linkedToken) |> Async.AwaitTask
            use! transaction = connection.BeginTransactionAsync(isolationLevel, linkedToken).AsTask() |> Async.AwaitTask
            match! action() with
            | Some some ->
                do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
                return Some some
            | None ->
                return None
        finally
            if wasClosed then connection.Close()
    }

let commitOnOk (connection: #DbConnection) cancellationToken isolationLevel action =
    async {
        let wasClosed = connection.State = ConnectionState.Closed
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! connection.OpenAsync(linkedToken) |> Async.AwaitTask
            use! transaction = connection.BeginTransactionAsync(isolationLevel, linkedToken).AsTask() |> Async.AwaitTask
            match! action() with
            | Ok ok ->
                do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
                return Ok ok
            | Error error ->
                return Error error
        finally
            if wasClosed then connection.Close()
    }

let defaultCommit connection action =
    commit connection CancellationToken.None DefaultIsolationLevel action

let defaultNotCommit connection action =
    notCommit connection CancellationToken.None DefaultIsolationLevel action

let defaultCommitOnSome connection action =
    commitOnSome connection CancellationToken.None DefaultIsolationLevel action

let defaultCommitOnOk connection action =
    commitOnOk connection CancellationToken.None DefaultIsolationLevel action
