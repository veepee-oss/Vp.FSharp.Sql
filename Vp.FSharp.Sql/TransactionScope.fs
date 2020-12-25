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

let private newTransactionScope isolationLevel timeout scopeOption =
    let mutable transactionOptions = TransactionOptions()
    transactionOptions.Timeout <- timeout
    transactionOptions.IsolationLevel <- isolationLevel
    new TransactionScope(scopeOption, transactionOptions, TransactionScopeAsyncFlowOption.Enabled)

let private execute isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    async {
        use transactionScope = newTransactionScope isolationLevel timeout scopeOption
        connection.EnlistCurrentTransaction()
        let! applyOutcome = body connection
        return (transactionScope, applyOutcome)
    }

let private execute2 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        use transactionScope = newTransactionScope isolationLevel timeout scopeOption
        connection1.EnlistCurrentTransaction()
        connection2.EnlistCurrentTransaction()
        let! applyOutcome = body connection1 connection2
        return (transactionScope, applyOutcome)
    }

let private execute3 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    async {
        use transactionScope = newTransactionScope isolationLevel timeout scopeOption
        connection1.EnlistCurrentTransaction()
        connection2.EnlistCurrentTransaction()
        connection3.EnlistCurrentTransaction()
        let! applyOutcome = body connection1 connection2 connection3
        return (transactionScope, applyOutcome)
    }

let complete isolationLevel timeout scopeOption (connection: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute isolationLevel timeout scopeOption connection body
        transactionScope.Complete()
        return applyOutcome
    }
let complete2 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute2 isolationLevel timeout scopeOption connection1 connection2 body
        transactionScope.Complete()
        return applyOutcome
    }
let complete3 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute3 isolationLevel timeout scopeOption connection1 connection2 connection3 body
        transactionScope.Complete()
        return applyOutcome
    }

let notComplete isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    async {
        let! (_, applyOutcome) =
            execute isolationLevel timeout scopeOption connection body
        return! applyOutcome
    }
let notComplete2 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        let! (_, applyOutcome) =
            execute2 isolationLevel timeout scopeOption connection1 connection2 body
        return! applyOutcome
    }
let notComplete3 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    async {
        let! (_, applyOutcome) =
            execute3 isolationLevel timeout scopeOption connection1 connection2 connection3 body
        return! applyOutcome
    }

let completeOnSome isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute isolationLevel timeout scopeOption connection body
        match applyOutcome with
        | Some some ->
            transactionScope.Complete()
            return Some some
        | None ->
            return None
    }
let completeOnSome2 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute2 isolationLevel timeout scopeOption connection1 connection2 body
        match applyOutcome with
        | Some some ->
            transactionScope.Complete()
            return Some some
        | None ->
            return None
    }
let completeOnSome3 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute3 isolationLevel timeout scopeOption connection1 connection2 connection3 body
        match applyOutcome with
        | Some some ->
            transactionScope.Complete()
            return Some some
        | None ->
            return None
    }

let completeOnOk isolationLevel timeout scopeOption
    (connection: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute isolationLevel timeout scopeOption connection body
        match applyOutcome with
        | Ok ok ->
            transactionScope.Complete()
            return Ok ok
        | Error error ->
            return Error error
    }
let completeOnOk2 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute2 isolationLevel timeout scopeOption connection1 connection2 body
        match applyOutcome with
        | Ok ok ->
            transactionScope.Complete()
            return Ok ok
        | Error error ->
            return Error error
    }
let completeOnOk3 isolationLevel timeout scopeOption
    (connection1: #DbConnection) (connection2: #DbConnection) (connection3: #DbConnection) body =
    async {
        let! (transactionScope, applyOutcome) =
            execute3 isolationLevel timeout scopeOption connection1 connection2 connection3 body
        match applyOutcome with
        | Ok ok ->
            transactionScope.Complete()
            return Ok ok
        | Error error ->
            return Error error
    }

let defaultComplete body =
    body
    |> complete DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultComplete2 body =
    body
    |> complete2 DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultComplete3 body =
    body
    |> complete3 DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultNotComplete body =
    body
    |> notComplete DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultNotComplete2 body =
    body
    |> notComplete2 DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultNotComplete3 body =
    body
    |> notComplete3 DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultCompleteOnSome body =
    body
    |> completeOnSome DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultCompleteOnSome2 body =
    body
    |> completeOnSome2 DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultCompleteOnSome3 body =
    body
    |> completeOnSome3 DefaultIsolationLevel defaultTimeout DefaultScopeOption

let defaultCompleteOnOk body =
    body
    |> completeOnOk DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultCompleteOnOk2 body =
    body
    |> completeOnOk2 DefaultIsolationLevel defaultTimeout DefaultScopeOption
let defaultCompleteOnOk3 body =
    body
    |> completeOnOk3 DefaultIsolationLevel defaultTimeout DefaultScopeOption
