[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.TransactionScope

open System
open System.Threading
open System.Data.Common
open System.Transactions

open Vp.FSharp.Sql.Helpers


[<Literal>]
let DefaultIsolationLevel = IsolationLevel.ReadCommitted

[<Literal>]
let DefaultScopeOption = TransactionScopeOption.Required

[<Literal>]
let DefaultTimeoutInSeconds = 30.

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

let defaultComplete body =
    body
    |> complete CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultComplete2 body =
    body
    |> complete2 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultComplete3 body =
    body
    |> complete3 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultNotComplete body =
    body
    |> notComplete CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultNotComplete2 body =
    body
    |> notComplete2 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultNotComplete3 body =
    body
    |> notComplete3 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultCompleteOnSome body =
    body
    |> completeOnSome CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultCompleteOnSome2 body =
    body
    |> completeOnSome2 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultCompleteOnSome3 body =
    body
    |> completeOnSome3 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultCompleteOnOk body =
    body
    |> completeOnOk CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultCompleteOnOk2 body =
    body
    |> completeOnOk2 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultCompleteOnOk3 body =
    body
    |> completeOnOk3 CancellationToken.None DefaultIsolationLevel defaultTimeout DefaultScopeOption
