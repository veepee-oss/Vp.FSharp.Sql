[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.TransactionScope

open System
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

let private execute isolationLevel timeout scopeOption (connection: #DbConnection) apply =
    async {
        let mutable transactionOptions = TransactionOptions()
        transactionOptions.Timeout <- timeout
        transactionOptions.IsolationLevel <- isolationLevel
        use transactionScope = new TransactionScope(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled)
        connection.EnlistCurrentTransaction()
        let! applyOutcome = apply connection
        return (transactionScope, applyOutcome)
    }

let complete isolationLevel timeout scopeOption (connection: #DbConnection) apply =
    async {
        let! (transactionScope, applyOutcome) = execute isolationLevel timeout scopeOption connection apply
        transactionScope.Complete()
        return applyOutcome
    }

let notComplete isolationLevel timeout scopeOption (connection: #DbConnection) apply =
    async {
        let! (_, applyOutcome) = execute isolationLevel timeout scopeOption connection apply
        return! applyOutcome
    }

let completeOnSome isolationLevel timeout scopeOption (connection: #DbConnection) apply =
    async {
        let! (transactionScope, applyOutcome) = execute isolationLevel timeout scopeOption connection apply
        match applyOutcome with
        | Some some ->
            transactionScope.Complete()
            return Some some
        | None ->
            return None
    }

let completeOnOk isolationLevel timeout scopeOption (connection: #DbConnection) apply =
    async {
        let! (transactionScope, applyOutcome) = execute isolationLevel timeout scopeOption connection apply
        match applyOutcome with
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
