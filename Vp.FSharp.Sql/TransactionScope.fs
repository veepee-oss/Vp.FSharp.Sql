[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.TransactionScope

open System
open System.Transactions


[<Literal>]
let DefaultIsolationLevel = IsolationLevel.ReadCommitted

[<Literal>]
let DefaultScopeOption = TransactionScopeOption.RequiresNew

[<Literal>]
let DefaultTimeoutInSeconds = 30.

let defaultTimeout = TimeSpan.FromSeconds(DefaultTimeoutInSeconds)

let complete isolationLevel timeout scopeOption transaction =
    async {
        let mutable transactionOptions = TransactionOptions()
        transactionOptions.Timeout <- timeout
        transactionOptions.IsolationLevel <- isolationLevel
        use transactionScope = new TransactionScope(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled)
        let! operationsResult = transaction
        transactionScope.Complete()
        return operationsResult
    }

let notComplete isolationLevel timeout scopeOption transaction =
    async {
        let mutable transactionOptions = TransactionOptions()
        transactionOptions.Timeout <- timeout
        transactionOptions.IsolationLevel <- isolationLevel
        use _ = new TransactionScope(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled)
        let! operationsResult = transaction
        return operationsResult
    }

let completeOnSome isolationLevel timeout scopeOption transaction =
    async {
        let mutable transactionOptions = TransactionOptions()
        transactionOptions.Timeout <- timeout
        transactionOptions.IsolationLevel <- isolationLevel
        use transactionScope = new TransactionScope(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled)
        let! operationsResult = transaction
        match operationsResult with
        | Some some ->
            transactionScope.Complete()
            return Some some
        | None ->
            return None
    }

let completeOnOk isolationLevel timeout scopeOption transaction =
    async {
        let mutable transactionOptions = TransactionOptions()
        transactionOptions.Timeout <- timeout
        transactionOptions.IsolationLevel <- isolationLevel
        use transactionScope = new TransactionScope(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled)
        let! operationsResult = transaction
        match operationsResult with
        | Ok ok ->
            transactionScope.Complete()
            return Ok ok
        | Error error ->
            return Error error
    }

let defaultComplete transaction =
    transaction
    |> complete DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultNotComplete transaction =
    transaction
    |> notComplete DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultCompleteOnSome transaction =
    transaction
    |> completeOnSome DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultCompleteOnOk transaction =
    transaction
    |> completeOnOk DefaultIsolationLevel defaultTimeout DefaultScopeOption
