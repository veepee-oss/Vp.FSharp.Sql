[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.TransactionScope

open System
open System.Threading
open System.Data.Common
open System.Transactions

open Vp.FSharp.Sql.Helpers


/// Isolation level used for defaultXXX functions
[<Literal>]
let DefaultIsolationLevel = IsolationLevel.ReadCommitted

/// Scope option used for defaultXXX functions
[<Literal>]
let DefaultScopeOption = TransactionScopeOption.Required

/// Timeout in seconds used for defaultXXX functions
[<Literal>]
let DefaultTimeoutInSeconds = 30.

/// Timeout as timespan used for defaultXXX functions
let defaultTimeout = TimeSpan.FromSeconds(DefaultTimeoutInSeconds)

let private newTransactionScope isolationLevel timeout scopeOption =
    let mutable transactionOptions = TransactionOptions()
    transactionOptions.Timeout <- timeout
    transactionOptions.IsolationLevel <- isolationLevel
    new TransactionScope(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled)

let private startScope isolationLevel timeout scopeOption
    (connection: #DbConnection) =
    let transactionScope = newTransactionScope isolationLevel timeout scopeOption
    DbConnection.enlistCurrentTransaction connection
    transactionScope

let private startScope2 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) =
    let transactionScope = newTransactionScope isolationLevel timeout scopeOption
    DbConnection.enlistCurrentTransaction connection1
    DbConnection.enlistCurrentTransaction connection2
    transactionScope

let private startScope3 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) =
    let transactionScope = newTransactionScope isolationLevel timeout scopeOption
    DbConnection.enlistCurrentTransaction connection1
    DbConnection.enlistCurrentTransaction connection2
    DbConnection.enlistCurrentTransaction connection3
    transactionScope

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, connection and transaction body.
/// This function runs asynchronously.
let complete cancellationToken isolationLevel timeout scopeOption (connection: #DbConnection) body =
    async {
        let closed = DbConnection.isClosed connection
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed connection

            use transactionScope = startScope isolationLevel timeout scopeOption connection
            let! applyOutcome = body connection
            transactionScope.Complete()
            return applyOutcome

        finally
            DbConnection.closedIfClosed closed connection
    }

/// Create and commit an automatically generated transaction scope with the given
/// timeout, scope option, connection and transaction body.
/// This function runs synchronously.
let completeSync isolationLevel timeout scopeOption (connection: #DbConnection) body =
    let closed = DbConnection.isClosed connection
    try
        DbConnection.openIfClosedSync closed connection

        use transactionScope = startScope isolationLevel timeout scopeOption connection
        let applyOutcome = body connection
        transactionScope.Complete()
        applyOutcome

    finally
        DbConnection.closedIfClosed closed connection

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, 2 connections and transaction body.
/// This function runs asynchronously.
let complete2 cancellationToken isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        let closed1 = DbConnection.isClosed connection1
        let closed2 = DbConnection.isClosed connection2
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed1 connection1
            do! DbConnection.openIfClosed linkedToken closed2 connection2

            use transactionScope = startScope2 isolationLevel timeout scopeOption connection1 connection2
            let! applyOutcome = body connection1 connection2

            transactionScope.Complete()

            return applyOutcome
        finally
            DbConnection.closedIfClosed closed1 connection1
            DbConnection.closedIfClosed closed2 connection2
    }

/// Create and commit an automatically generated transaction scope with the given
/// timeout, scope option, 2 connections and transaction body.
/// This function runs synchronously.
let complete2Sync isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    let closed1 = DbConnection.isClosed connection1
    let closed2 = DbConnection.isClosed connection2
    try
        DbConnection.openIfClosedSync closed1 connection1
        DbConnection.openIfClosedSync closed2 connection2

        use transactionScope = startScope2 isolationLevel timeout scopeOption connection1 connection2
        let applyOutcome = body connection1 connection2
        transactionScope.Complete()
        applyOutcome
    finally
        DbConnection.closedIfClosed closed1 connection1
        DbConnection.closedIfClosed closed2 connection2

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, 3 connections and transaction body.
/// This function runs asynchronously.
let complete3 cancellationToken isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    async {
        let closed1 = DbConnection.isClosed connection1
        let closed2 = DbConnection.isClosed connection2
        let closed3 = DbConnection.isClosed connection3
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed1 connection1
            do! DbConnection.openIfClosed linkedToken closed2 connection2
            do! DbConnection.openIfClosed linkedToken closed3 connection3

            use transactionScope = startScope3 isolationLevel timeout scopeOption connection1 connection2 connection3
            let! applyOutcome = body connection1 connection2 connection3
            transactionScope.Complete()
            return applyOutcome
        finally
            DbConnection.closedIfClosed closed1 connection1
            DbConnection.closedIfClosed closed2 connection2
            DbConnection.closedIfClosed closed3 connection3
    }

/// Create and commit an automatically generated transaction scope with the given
/// timeout, scope option, 3 connections and transaction body.
/// This function runs synchronously.
let complete3Sync isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    let closed1 = DbConnection.isClosed connection1
    let closed2 = DbConnection.isClosed connection2
    let closed3 = DbConnection.isClosed connection3
    try
        DbConnection.openIfClosedSync closed1 connection1
        DbConnection.openIfClosedSync closed2 connection2
        DbConnection.openIfClosedSync closed3 connection3

        use transactionScope = startScope3 isolationLevel timeout scopeOption connection1 connection2 connection3
        let applyOutcome = body connection1 connection2 connection3
        transactionScope.Complete()
        applyOutcome
    finally
        DbConnection.closedIfClosed closed1 connection1
        DbConnection.closedIfClosed closed2 connection2
        DbConnection.closedIfClosed closed3 connection3

/// Create and do not commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, connection and transaction body.
/// This function runs asynchronously.
let notComplete cancellationToken isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    async {
        let closed = DbConnection.isClosed connection
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed connection

            use _ = startScope isolationLevel timeout scopeOption connection
            return! body connection
        finally
            DbConnection.closedIfClosed closed connection
    }

/// Create and do not commit an automatically generated transaction scope with the given
/// timeout, scope option, connection and transaction body.
/// This function runs synchronously.
let notCompleteSync isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    let closed = DbConnection.isClosed connection
    try
        DbConnection.openIfClosedSync closed connection

        use _transaction = startScope isolationLevel timeout scopeOption connection
        body connection
    finally
        DbConnection.closedIfClosed closed connection

/// Create and do not commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, 2 connections and transaction body.
/// This function runs asynchronously.
let notComplete2 cancellationToken isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        let closed1 = DbConnection.isClosed connection1
        let closed2 = DbConnection.isClosed connection2
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed1 connection1
            do! DbConnection.openIfClosed linkedToken closed2 connection2

            use _ = startScope2 isolationLevel timeout scopeOption connection1 connection2
            return! body connection1 connection2
        finally
            DbConnection.closedIfClosed closed1 connection1
            DbConnection.closedIfClosed closed2 connection2
    }

/// Create and do not commit an automatically generated transaction scope with the given
/// timeout, scope option, 2 connections and transaction body.
/// This function runs synchronously.
let notComplete2Sync isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    let closed1 = DbConnection.isClosed connection1
    let closed2 = DbConnection.isClosed connection2
    try
        DbConnection.openIfClosedSync closed1 connection1
        DbConnection.openIfClosedSync closed2 connection2

        use _transaction = startScope2 isolationLevel timeout scopeOption connection1 connection2
        body connection1 connection2
    finally
        DbConnection.closedIfClosed closed1 connection1
        DbConnection.closedIfClosed closed2 connection2

/// Create and do not commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, 3 connections and transaction body.
/// This function runs asynchronously.
let notComplete3 cancellationToken isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection)
    body =
    async {
        let closed1 = DbConnection.isClosed connection1
        let closed2 = DbConnection.isClosed connection2
        let closed3 = DbConnection.isClosed connection3
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed1 connection1
            do! DbConnection.openIfClosed linkedToken closed2 connection2
            do! DbConnection.openIfClosed linkedToken closed3 connection3

            use _ = startScope3 isolationLevel timeout scopeOption connection1 connection2 connection3
            return! body connection1 connection2 connection3
        finally
            DbConnection.closedIfClosed closed1 connection1
            DbConnection.closedIfClosed closed2 connection2
            DbConnection.closedIfClosed closed3 connection3
    }

/// Create and do not commit an automatically generated transaction scope with the given
/// timeout, scope option, 3 connections and transaction body.
/// This function runs synchronously.
let notComplete3Sync isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection)
    body =
    let closed1 = DbConnection.isClosed connection1
    let closed2 = DbConnection.isClosed connection2
    let closed3 = DbConnection.isClosed connection3
    try
        DbConnection.openIfClosedSync closed1 connection1
        DbConnection.openIfClosedSync closed2 connection2
        DbConnection.openIfClosedSync closed3 connection3

        use _transaction = startScope3 isolationLevel timeout scopeOption connection1 connection2 connection3
        body connection1 connection2 connection3
    finally
        DbConnection.closedIfClosed closed1 connection1
        DbConnection.closedIfClosed closed2 connection2
        DbConnection.closedIfClosed closed3 connection3

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, connection and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs asynchronously.
let completeOnSome cancellationToken isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    async {
        let closed = DbConnection.isClosed connection
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed connection

            use transactionScope = startScope isolationLevel timeout scopeOption connection
            let! applyOutcome = body connection
            match applyOutcome with
            | Some some ->
                transactionScope.Complete()
                return Some some
            | None ->
                return None
        finally
            DbConnection.closedIfClosed closed connection
    }

/// Create and commit an automatically generated transaction scope with the given
/// token, timeout, scope option, connection and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let completeOnSomeSync isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    let closed = DbConnection.isClosed connection
    try
        DbConnection.openIfClosedSync closed connection

        use transactionScope = startScope isolationLevel timeout scopeOption connection
        let applyOutcome = body connection
        match applyOutcome with
        | Some some ->
            transactionScope.Complete()
            Some some
        | None ->
            None
    finally
        DbConnection.closedIfClosed closed connection

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, 2 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs asynchronously.
let completeOnSome2 cancellationToken isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        let closed1 = DbConnection.isClosed connection1
        let closed2 = DbConnection.isClosed connection2
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed1 connection1
            do! DbConnection.openIfClosed linkedToken closed2 connection2

            use transactionScope = startScope2 isolationLevel timeout scopeOption connection1 connection2
            let! applyOutcome = body connection1 connection2
            match applyOutcome with
            | Some some ->
                transactionScope.Complete()
                return Some some
            | None ->
                return None
        finally
            DbConnection.closedIfClosed closed1 connection1
            DbConnection.closedIfClosed closed2 connection2
    }

/// Create and commit an automatically generated transaction scope with the given
/// timeout, scope option, 2 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let completeOnSome2Sync isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    let closed1 = DbConnection.isClosed connection1
    let closed2 = DbConnection.isClosed connection2
    try
        DbConnection.openIfClosedSync closed1 connection1
        DbConnection.openIfClosedSync closed2 connection2

        use transactionScope = startScope2 isolationLevel timeout scopeOption connection1 connection2
        let applyOutcome = body connection1 connection2
        match applyOutcome with
        | Some some ->
            transactionScope.Complete()
            Some some
        | None ->
            None
    finally
        DbConnection.closedIfClosed closed1 connection1
        DbConnection.closedIfClosed closed2 connection2

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, 3 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs asynchronously.
let completeOnSome3 cancellationToken isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    async {
        let closed1 = DbConnection.isClosed connection1
        let closed2 = DbConnection.isClosed connection2
        let closed3 = DbConnection.isClosed connection3
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed1 connection1
            do! DbConnection.openIfClosed linkedToken closed2 connection2
            do! DbConnection.openIfClosed linkedToken closed3 connection3

            use transactionScope = startScope3 isolationLevel timeout scopeOption connection1 connection2 connection3
            let! applyOutcome = body connection1 connection2 connection3
            match applyOutcome with
            | Some some ->
                transactionScope.Complete()
                return Some some
            | None ->
                return None
        finally
            DbConnection.closedIfClosed closed1 connection1
            DbConnection.closedIfClosed closed2 connection2
            DbConnection.closedIfClosed closed3 connection3
    }

/// Create and commit an automatically generated transaction scope with the given
/// timeout, scope option, 3 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let completeOnSome3Sync isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    let closed1 = DbConnection.isClosed connection1
    let closed2 = DbConnection.isClosed connection2
    let closed3 = DbConnection.isClosed connection3
    try
        DbConnection.openIfClosedSync closed1 connection1
        DbConnection.openIfClosedSync closed2 connection2
        DbConnection.openIfClosedSync closed3 connection3

        use transactionScope = startScope3 isolationLevel timeout scopeOption connection1 connection2 connection3
        let applyOutcome = body connection1 connection2 connection3
        match applyOutcome with
        | Some some ->
            transactionScope.Complete()
            Some some
        | None ->
            None
    finally
        DbConnection.closedIfClosed closed1 connection1
        DbConnection.closedIfClosed closed2 connection2
        DbConnection.closedIfClosed closed3 connection3

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, connection and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs asynchronously.
let completeOnOk cancellationToken isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    async {
        let closed = DbConnection.isClosed connection
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed connection

            use transactionScope = startScope isolationLevel timeout scopeOption connection
            let! applyOutcome = body connection
            match applyOutcome with
            | Ok ok ->
                transactionScope.Complete()
                return Ok ok
            | Error error ->
                return Error error
        finally
            DbConnection.closedIfClosed closed connection
    }

/// Create and commit an automatically generated transaction scope with the given
/// timeout, scope option, connection and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs synchronously.
let completeOnOkSync isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    let closed = DbConnection.isClosed connection
    try
        DbConnection.openIfClosedSync closed connection

        use transactionScope = startScope isolationLevel timeout scopeOption connection
        let applyOutcome = body connection
        match applyOutcome with
        | Ok ok ->
            transactionScope.Complete()
            Ok ok
        | Error error ->
            Error error
    finally
        DbConnection.closedIfClosed closed connection

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, 2 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs asynchronously.
let completeOnOk2 cancellationToken isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        let closed1 = DbConnection.isClosed connection1
        let closed2 = DbConnection.isClosed connection2
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed1 connection1
            do! DbConnection.openIfClosed linkedToken closed2 connection2

            use transactionScope = startScope2 isolationLevel timeout scopeOption connection1 connection2
            let! applyOutcome = body connection1 connection2
            match applyOutcome with
            | Ok ok ->
                transactionScope.Complete()
                return Ok ok
            | Error error ->
                return Error error
        finally
            DbConnection.closedIfClosed closed1 connection1
            DbConnection.closedIfClosed closed2 connection2
    }

/// Create and commit an automatically generated transaction scope with the given
/// timeout, scope option, 2 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs synchronously.
let completeOnOk2Sync isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    let closed1 = DbConnection.isClosed connection1
    let closed2 = DbConnection.isClosed connection2
    try
        DbConnection.openIfClosedSync closed1 connection1
        DbConnection.openIfClosedSync closed2 connection2

        use transactionScope = startScope2 isolationLevel timeout scopeOption connection1 connection2
        let applyOutcome = body connection1 connection2
        match applyOutcome with
        | Ok ok ->
            transactionScope.Complete()
            Ok ok
        | Error error ->
            Error error
    finally
        DbConnection.closedIfClosed closed1 connection1
        DbConnection.closedIfClosed closed2 connection2

/// Create and commit an automatically generated transaction scope with the given
/// cancellation token, timeout, scope option, 3 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs asynchronously.
let completeOnOk3 cancellationToken isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    async {
        let closed1 = DbConnection.isClosed connection1
        let closed2 = DbConnection.isClosed connection2
        let closed3 = DbConnection.isClosed connection3
        let! linkedToken = Async.linkedTokenSourceFrom cancellationToken
        try
            do! DbConnection.openIfClosed linkedToken closed1 connection1
            do! DbConnection.openIfClosed linkedToken closed2 connection2
            do! DbConnection.openIfClosed linkedToken closed3 connection3

            use transactionScope = startScope3 isolationLevel timeout scopeOption connection1 connection2 connection3
            let! applyOutcome = body connection1 connection2 connection3
            match applyOutcome with
            | Ok ok ->
                transactionScope.Complete()
                return Ok ok
            | Error error ->
                return Error error
        finally
            DbConnection.closedIfClosed closed1 connection1
            DbConnection.closedIfClosed closed2 connection2
            DbConnection.closedIfClosed closed3 connection3
    }

/// Create and commit an automatically generated transaction scope with the given
/// timeout, scope option, 3 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs synchronously.
let completeOnOk3Sync isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    let closed1 = DbConnection.isClosed connection1
    let closed2 = DbConnection.isClosed connection2
    let closed3 = DbConnection.isClosed connection3

    try
        DbConnection.openIfClosedSync closed1 connection1
        DbConnection.openIfClosedSync closed2 connection2
        DbConnection.openIfClosedSync closed3 connection3

        use transactionScope = startScope3 isolationLevel timeout scopeOption connection1 connection2 connection3
        let applyOutcome = body connection1 connection2 connection3
        match applyOutcome with
        | Ok ok ->
            transactionScope.Complete()
            Ok ok
        | Error error ->
            Error error
    finally
        DbConnection.closedIfClosed closed1 connection1
        DbConnection.closedIfClosed closed2 connection2
        DbConnection.closedIfClosed closed3 connection3

/// Create and commit an automatically generated transaction scope with the given connection and transaction body.
/// This function runs asynchronously.
let defaultComplete body =
    body
    |> complete CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given connection and transaction body.
/// This function runs synchronously.
let defaultCompleteSync body =
    body
    |> completeSync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 2 connections and transaction body.
/// This function runs asynchronously.
let defaultComplete2 body =
    body
    |> complete2 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 2 connections and transaction body.
/// This function runs synchronously.
let defaultComplete2Sync body =
    body
    |> complete2Sync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 3 connections and transaction body.
/// This function runs asynchronously.
let defaultComplete3 body =
    body
    |> complete3 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 3 connections and transaction body.
/// This function runs synchronously.
let defaultComplete3Sync body =
    body
    |> complete3Sync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and do not commit an automatically generated transaction scope with the given connection and transaction body.
/// This function runs asynchronously.
let defaultNotComplete body =
    body
    |> notComplete CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and do not commit an automatically generated transaction scope with the given connection and transaction body.
/// This function runs synchronously.
let defaultNotCompleteSync body =
    body
    |> notCompleteSync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and do not commit an automatically generated transaction scope with the given 2 connections and transaction body.
/// This function runs asynchronously.
let defaultNotComplete2 body =
    body
    |> notComplete2 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and do not commit an automatically generated transaction scope with the given 2 connections and transaction body.
/// This function runs synchronously.
let defaultNotComplete2Sync body =
    body
    |> notComplete2Sync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and do not commit an automatically generated transaction scope with the given 3 connections and transaction body.
/// This function runs synchronously.
let defaultNotComplete3 body =
    body
    |> notComplete3 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and do not commit an automatically generated transaction scope with the given 3 connections and transaction body.
/// This function runs synchronously.
let defaultNotComplete3Sync body =
    body
    |> notComplete3Sync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let defaultCompleteOnSome body =
    body
    |> completeOnSome CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let defaultCompleteOnSomeSync body =
    body
    |> completeOnSomeSync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 2 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs asynchronously.
let defaultCompleteOnSome2 body =
    body
    |> completeOnSome2 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 2 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let defaultCompleteOnSome2Sync body =
    body
    |> completeOnSome2Sync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 3 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs asynchronously.
let defaultCompleteOnSome3 body =
    body
    |> completeOnSome3 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 3 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let defaultCompleteOnSome3Sync body =
    body
    |> completeOnSome3Sync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs asynchronously.
let defaultCompleteOnOk body =
    body
    |> completeOnOk CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs synchronously.
let defaultCompleteOnOkSync body =
    body
    |> completeOnOkSync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 2 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs asynchronously.
let defaultCompleteOnOk2 body =
    body
    |> completeOnOk2 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 2 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs synchronously.
let defaultCompleteOnOk2Sync body =
    body
    |> completeOnOk2Sync DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 3 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs asynchronously.
let defaultCompleteOnOk3 body =
    body
    |> completeOnOk3 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

/// Create and commit an automatically generated transaction scope with the given 3 connections and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs synchronously.
let defaultCompleteOnOk3Sync body =
    body
    |> completeOnOk3Sync DefaultIsolationLevel defaultTimeout DefaultScopeOption
