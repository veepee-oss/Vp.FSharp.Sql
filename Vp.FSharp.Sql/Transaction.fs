[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.Transaction

open System.Data
open System.Threading
open System.Data.Common
open System.Threading.Tasks

open Vp.FSharp.Sql.Helpers


[<Literal>]
let DefaultIsolationLevel = IsolationLevel.ReadCommitted

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// cancellation token and transaction body.
/// Note: This function runs asynchronously.
let commit cancellationToken isolationLevel
    (connection: #DbConnection)
    (beginTransaction: #DbConnection -> IsolationLevel -> CancellationToken -> ValueTask<#DbTransaction>)
    (body: #DbConnection -> #DbTransaction -> Async<'Output>)=
    async {
        let wasClosed = DbConnection.isClosed connection
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! DbConnection.openIfClosed linkedToken wasClosed connection
            use! transaction = beginTransaction connection isolationLevel linkedToken |> Async.AwaitValueTask
            let! actionResult = body connection transaction
            do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
            return actionResult
        finally
            DbConnection.closedIfClosed wasClosed connection
    }

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// and transaction body.
/// Note: This function runs synchronously.
let commitSync isolationLevel
    (connection: #DbConnection)
    (beginTransaction: #DbConnection -> IsolationLevel -> #DbTransaction)
    (body: #DbConnection -> #DbTransaction -> 'Output)=
    let wasClosed = DbConnection.isClosed connection
    try
        if wasClosed then DbConnection.openIfClosedSync wasClosed connection
        use transaction = beginTransaction connection isolationLevel
        let actionResult = body connection transaction
        transaction.Commit()
        actionResult
    finally
        DbConnection.closedIfClosed wasClosed connection

/// Create and do not commit an automatically generated transaction with the given connection, isolation,
/// cancellation token and transaction body.
/// Note: This function runs asynchronously.
let notCommit cancellationToken isolationLevel
    (connection: #DbConnection)
    (beginTransaction: #DbConnection -> IsolationLevel -> CancellationToken -> ValueTask<#DbTransaction>)
    (body: #DbConnection -> #DbTransaction -> Async<'Output>) =
    async {
        let wasClosed = DbConnection.isClosed connection
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! DbConnection.openIfClosed linkedToken wasClosed connection
            use! transaction = beginTransaction connection isolationLevel linkedToken |> Async.AwaitValueTask
            let! actionResult = body connection transaction
            return actionResult
        finally
            DbConnection.closedIfClosed wasClosed connection
    }

/// Create and do not commit an automatically generated transaction with the given connection, isolation,
/// and transaction body.
/// Note: This function runs synchronously.
let notCommitSync isolationLevel
    (connection: #DbConnection)
    (beginTransaction: #DbConnection -> IsolationLevel -> #DbTransaction)
    (body: #DbConnection -> #DbTransaction -> 'Output) =
        let wasClosed = DbConnection.isClosed connection
        try
            if wasClosed then DbConnection.openIfClosedSync wasClosed connection
            use transaction = beginTransaction connection isolationLevel
            let actionResult = body connection transaction
            actionResult
        finally
            DbConnection.closedIfClosed wasClosed connection

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// cancellation token and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// Note: This function runs asynchronously.
let commitOnSome cancellationToken isolationLevel
    (connection: #DbConnection)
    (beginTransaction: #DbConnection -> IsolationLevel -> CancellationToken -> ValueTask<#DbTransaction>)
    (body: #DbConnection -> #DbTransaction -> Async<'Output option>) =
    async {
        let wasClosed = DbConnection.isClosed connection
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! DbConnection.openIfClosed linkedToken wasClosed connection
            use! transaction = beginTransaction connection isolationLevel linkedToken |> Async.AwaitValueTask
            match! body connection transaction with
            | Some some ->
                do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
                return Some some
            | None ->
                return None
        finally
            DbConnection.closedIfClosed wasClosed connection
    }

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// Note: This function runs synchronously.
let commitOnSomeSync isolationLevel
    (connection: #DbConnection)
    (beginTransaction: #DbConnection -> IsolationLevel -> #DbTransaction)
    (body: #DbConnection -> #DbTransaction -> 'Output option) =
    let wasClosed = DbConnection.isClosed connection
    try
        if wasClosed then DbConnection.openIfClosedSync wasClosed connection
        use transaction = beginTransaction connection isolationLevel
        match body connection transaction with
        | Some some ->
            transaction.Commit()
            Some some
        | None ->
            None
    finally
        DbConnection.closedIfClosed wasClosed connection

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// cancellation token and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// Note: This function runs asynchronously.
let commitOnOk cancellationToken isolationLevel
    (connection: #DbConnection)
    (beginTransaction: #DbConnection -> IsolationLevel -> CancellationToken -> ValueTask<#DbTransaction>)
    (body: #DbConnection -> #DbTransaction -> Async<Result<'Ok, 'Error>>) =
    async {
        let wasClosed = DbConnection.isClosed connection
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            if wasClosed then do! DbConnection.openIfClosed linkedToken wasClosed connection
            use! transaction = beginTransaction connection isolationLevel linkedToken |> Async.AwaitValueTask
            match! body connection transaction with
            | Ok ok ->
                do! transaction.CommitAsync(linkedToken) |> Async.AwaitTask
                return Ok ok
            | Error error ->
                return Error error
        finally
            DbConnection.closedIfClosed wasClosed connection
    }

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// Note: This function runs synchronously.
let commitOnOkSync isolationLevel
    (connection: #DbConnection)
    (beginTransaction: #DbConnection -> IsolationLevel -> #DbTransaction)
    (body: #DbConnection -> #DbTransaction -> Result<'Ok, 'Error>) =
    let wasClosed = DbConnection.isClosed connection
    try
        if wasClosed then DbConnection.openIfClosedSync wasClosed connection
        use transaction = beginTransaction connection isolationLevel
        match body connection transaction with
        | Ok ok ->
            transaction.Commit()
            Ok ok
        | Error error ->
            Error error
    finally
        DbConnection.closedIfClosed wasClosed connection

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// Note: This function runs asynchronously.
let defaultCommit connection beginTransaction body =
    commit CancellationToken.None DefaultIsolationLevel connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// Note: This function runs synchronously.
let defaultCommitSync connection beginTransaction body =
    commitSync DefaultIsolationLevel connection beginTransaction body

/// Create and do not commit an automatically generated transaction with the given connection and transaction body.
/// Note: This function runs asynchronously.
let defaultNotCommit connection beginTransaction body =
    notCommit CancellationToken.None DefaultIsolationLevel connection beginTransaction body

/// Create and do not commit an automatically generated transaction with the given connection and transaction body.
/// Note: This function runs synchronously.
let defaultNotCommitSync connection beginTransaction body =
    notCommitSync DefaultIsolationLevel connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// Note: This function runs asynchronously.
let defaultCommitOnSome connection beginTransaction body =
    commitOnSome CancellationToken.None DefaultIsolationLevel connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// Note: This function runs synchronously.
let defaultCommitOnSomeSync connection beginTransaction body =
    commitOnSomeSync DefaultIsolationLevel connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// Note: This function runs asynchronously.
let defaultCommitOnOk connection beginTransaction body =
    commitOnOk CancellationToken.None DefaultIsolationLevel connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// Note: This function runs synchronously.
let defaultCommitOnOkSync connection beginTransaction body =
    commitOnOkSync DefaultIsolationLevel connection beginTransaction body
