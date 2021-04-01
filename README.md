# `Vp.FSharp.Sql`

The core library that enables you to work with F# and any ADO provider, _consistently_.

In most cases, this library is only used for creating other F# libraries leveraging the relevant ADO providers.

If you just want to execute SQL commands a-la-F#, you might want to look at [this section](#-how-to-use-this-library).

# ✨ Slagging Hype

We follow "highly controversial practices" to the best of our ability!

Status     | Package                
---------- | ----------------------
OK         | [![Conventional Commits](https://img.shields.io/badge/Conventional%20Commits-1.0.0-green.svg)](https://conventionalcommits.org)
OK (sorta) | [![semver](https://img.shields.io/badge/semver-2.0.0-green)](https://semver.org/spec/v2.0.0.html)
TBD        | [![keep a changelog](https://img.shields.io/badge/keep%20a%20changelog-1.0.0-red)](https://keepachangelog.com/en/1.0.0)
TBD        | [![Semantic Release](https://img.shields.io/badge/Semantic%20Release-17.1.1-red)](https://semantic-release.gitbook.io/semantic-release)

[Conventional Commits]: https://conventionalcommits.org
[semver]: https://img.shields.io/badge/semver-2.0.0-blue
[Semantic Release]: https://semantic-release.gitbook.io/semantic-release
[keep a changelog]: https://keepachangelog.com/en/1.0.0

# 📦 NuGet Package

 Name            | Version  | Command |
---------------- | -------- | ------- |
 `Vp.FSharp.Sql` | [![NuGet Status](http://img.shields.io/nuget/v/Vp.FSharp.Sql.svg)](https://www.nuget.org/packages/Vp.FSharp.Sql) | `Install-Package Vp.FSharp.Sql`

# 📚 How to use this library?

This library mostly aims to be a foundation for building other libraries with the relevant ADO.NET providers to provide a strongly-typed experience.

You can check out the libraries below, each leveraging `Vp.FSharp.Sql` and the relevant ADO.NET provider:

Name                                          | ADO.NET Provider                                                                       | Version  | Command |
--------------------------------------------- | -------------------------------------------------------------------------------------- | -------- | ------- |
[`Vp.FSharp.Sql.Sqlite`][sqlite-repo]         | [`System.Data.SQLite.Core`](https://www.nuget.org/packages/System.Data.SQLite.Core)    | [![NuGet Status](http://img.shields.io/nuget/v/Vp.FSharp.Sql.Sqlite.svg)](https://www.nuget.org/packages/Vp.FSharp.Sql.Sqlite)       | `Install-Package Vp.FSharp.Sql.Sqlite`
[`Vp.FSharp.Sql.SqlServer`][sqlserver-repo]   | [`Microsoft.Data.SqlClient`](https://www.nuget.org/packages/Microsoft.Data.SqlClient)  | [![NuGet Status](http://img.shields.io/nuget/v/Vp.FSharp.Sql.SqlServer.svg)](https://www.nuget.org/packages/Vp.FSharp.Sql.SqlServer) | `Install-Package Vp.FSharp.Sql.SqlServer`
[`Vp.FSharp.Sql.PostgreSql`][postgresql-repo] | [`Npgsql`](https://www.nuget.org/packages/Npgsql)                                      | [![NuGet Status](http://img.shields.io/nuget/v/Vp.FSharp.Sql.PostgreSql.svg)](https://www.nuget.org/packages/Vp.FSharp.Sql.PostgreSql)   | `Install-Package Vp.FSharp.Sql.PostgreSql`

In a nutshell, you can create your own complete provider, but you're free to just go with only the things you need.

Let's walk-through the [`Vp.FSharp.Sql.Sqlite` provider implementation][sqlite-repo].

## 💿 Database Value Type

First you need the most important type of all, the database value type. 

In the case of SQLite, `SqliteDbValue` can modeled as a simple discriminated union (DU):

```fsharp
/// Native SQLite DB types.
/// See https://www.sqlite.org/datatype3.html
type SqliteDbValue =
    | Null
    | Integer of int64
    | Real of double
    | Text of string
    | Blob of byte array
```

These cases are created after [the official SQLite documentation](https://www.sqlite.org/datatype3.html).

## 💻 DB Value to DB Parameter Conversion

This is where we convert the DU exposed in the public API to an actual `DbParameter`-compatible class that can be consumed from the Core library functions.

In most scenarios, the implementation consists of writing pattern matches on the different database value type cases and creating the relevant `DbParameter` specific types available in the ADO.NET provider, if any:

```fsharp
let dbValueToParameter name value =
    let parameter = SQLiteParameter()
    parameter.ParameterName <- name
    match value with
    | Null ->
        parameter.TypeName <- (nameof Null).ToUpperInvariant()
    | Integer value ->
        parameter.TypeName <- (nameof Integer).ToUpperInvariant()
        parameter.Value <- value
    | Real value ->
        parameter.TypeName <- (nameof Real).ToUpperInvariant()
        parameter.Value <- value
    | Text value ->
        parameter.TypeName <- (nameof Text).ToUpperInvariant()
        parameter.Value <- value
    | Blob value ->
        parameter.TypeName <- (nameof Blob).ToUpperInvariant()
        parameter.Value <- value
    parameter
```

Note: this function doesn't have to be public, only the DU has to be public.

## 🔌 Binding Dependencies: Type and Function

The `SqlDependencies` acts like the glue that sticks all the most important underlying ADO-specific operations:

```fsharp
/// SQLite Dependencies
type SqliteDependencies =
    SqlDependencies<
        SQLiteConnection,
        SQLiteCommand,
        SQLiteParameter,
        SQLiteDataReader,
        SQLiteTransaction,
        SqliteDbValue>
```

An instance of this type can implemented with:
```fsharp
let beginTransactionAsync (connection: SQLiteConnection) (isolationLevel: IsolationLevel) _ =
    ValueTask.FromResult(connection.BeginTransaction(isolationLevel))

let executeReaderAsync (command: SQLiteCommand) _ =
    Task.FromResult(command.ExecuteReader())

{ CreateCommand = fun connection -> connection.CreateCommand()
  SetCommandTransaction = fun command transaction -> command.Transaction <- transaction
  BeginTransaction = fun connection -> connection.BeginTransaction
  BeginTransactionAsync = beginTransactionAsync
  ExecuteReader = fun command -> command.ExecuteReader()
  ExecuteReaderAsync = executeReaderAsync
  DbValueToParameter = Constants.DbValueToParameter }
```

In this particular case, `System.Data.SQLite`, the most specific types are only available through the non-asynchronous API.

For instance, we use `command.ExecuteReader` instead of `command.ExecuteDbDataReader` because the return type is the most specific one: 
- [`SQLiteCommand.ExecuteDbDataReader()`](https://github.com/haf/System.Data.SQLite/blob/master/System.Data.SQLite/SQLiteCommand.cs#L664-L667)
- [`SQLiteCommand.ExecuteReader()`](https://github.com/haf/System.Data.SQLite/blob/master/System.Data.SQLite/SQLiteCommand.cs#L868-L873)

Also, as you may have noticed there is no occurence of an asynchronous API, meaning that the asynchronous "implementation" (or lack of thereof) relies on the base class implementation: 
- [`DbCommand.ExecuteReaderAsync()`](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Data.Common/src/System/Data/Common/DbCommand.cs#L150-L151)
- [`DbCommand.ExecuteDbDataReader(CommandBehavior behavior)`](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Data.Common/src/System/Data/Common/DbCommand.cs#L107)

which is just an asynchronous wrapper around the synchronous version.

Similarly, when it comes to `connection.BeginTransaction` instead of `command.BeginTransactionAsync`:
- [`SQLiteConnection.BeginTransaction()`](https://github.com/haf/System.Data.SQLite/blob/master/System.Data.SQLite/SQLiteConnection.cs#L1474-L1478)
- [`DbConnection.BeginTransactionAsync()`](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Data.Common/src/System/Data/Common/DbConnection.cs#L84-L85)

This example alone shows the kind of discrepancies you can expect to find in the most available ADO.NET provider implementations.

## ⌨ Command Definition

For the sake of simplicity, you can constraint the `CommandDefinition` type with the relevant ADO provider types, as some sort of type binder: 

```fsharp
/// SQLite Command Definition
type SqliteCommandDefinition =
    CommandDefinition<
        SQLiteConnection,
        SQLiteCommand,
        SQLiteParameter,
        SQLiteDataReader,
        SQLiteTransaction,
        SqliteDbValue>
```

This can be later on used with the `SqlCommand` functions which accept `CommandDefinition` as one of their parameters. 

## 📀 Configuration

There is yet another specialization in terms of generic constraints:

```fsharp
/// SQLite Configuration
type SqliteConfiguration =
    SqlConfigurationCache<
        SQLiteConnection,
        SQLiteCommand>
```

This type is another binder for types and acts as a cache; it will be passed along with the command definition when executing a command.


## 🏗️ Command Construction

This is fairly straightforward, all you need to do is:
- Create a new module (if you want to).
- Define the construction functions relevant to your library and pass the command definition to the `SqlCommand` core functions.

```fsharp
[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.Sqlite.SqliteCommand

open Vp.FSharp.Sql


/// Initialize a new command definition with the given text contained in the given string.
let text value : SqliteCommandDefinition =
    SqlCommand.text value

/// Initialize a new command definition with the given text spanning over several strings (ie. list).
let textFromList value : SqliteCommandDefinition =
    SqlCommand.textFromList value

/// Update the command definition so that when executing the command, it doesn't use any logger.
/// Be it the default one (Global, if any.) or a previously overriden one.
let noLogger commandDefinition = { commandDefinition with Logger = LoggerKind.Nothing }

/// Update the command definition so that when executing the command, it use the given overriding logger.
/// instead of the default one, aka the Global logger, if any.
let overrideLogger value commandDefinition = { commandDefinition with Logger = LoggerKind.Override value }

/// Update the command definition with the given parameters.
let parameters value (commandDefinition: SqliteCommandDefinition) : SqliteCommandDefinition =
    SqlCommand.parameters value commandDefinition

/// Update the command definition with the given cancellation token.
let cancellationToken value (commandDefinition: SqliteCommandDefinition) : SqliteCommandDefinition =
    SqlCommand.cancellationToken value commandDefinition

/// Update the command definition with the given timeout.
/// Note: kludged because SQLite doesn't support per-command timeout values.
let timeout value (commandDefinition: SqliteCommandDefinition) : SqliteCommandDefinition =
    SqlCommand.timeout value commandDefinition

/// Update the command definition and sets whether the command should be prepared or not.
let prepare value (commandDefinition: SqliteCommandDefinition) : SqliteCommandDefinition =
    SqlCommand.prepare value commandDefinition

/// Update the command definition and sets whether the command should be wrapped in the given transaction.
let transaction value (commandDefinition: SqliteCommandDefinition) : SqliteCommandDefinition =
    SqlCommand.transaction value commandDefinition
```

## ⚙ Command Execution

Likewise, command execution follows the same principles, aka passing the relevant strongly-typed parameters (corresponding to your current and specific ADO.NET provider) to the SQLCommand core functions.

```fsharp

/// Execute the command and return the sets of rows as an AsyncSeq accordingly to the command definition.
/// This function runs asynchronously.
let queryAsyncSeq connection read (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.queryAsyncSeq
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read commandDefinition

/// Execute the command and return the sets of rows as an AsyncSeq accordingly to the command definition.
/// This function runs synchronously.
let querySeqSync connection read (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.querySeqSync
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read commandDefinition

/// Execute the command and return the sets of rows as a list accordingly to the command definition.
/// This function runs asynchronously.
let queryList connection read (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.queryList
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read commandDefinition

/// Execute the command and return the sets of rows as a list accordingly to the command definition.
/// This function runs synchronously.
let queryListSync connection read (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.queryListSync
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read commandDefinition

/// Execute the command and return the first set of rows as a list accordingly to the command definition.
/// This function runs asynchronously.
let querySetList connection read (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.querySetList
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read commandDefinition

/// Execute the command and return the first set of rows as a list accordingly to the command definition.
/// This function runs synchronously.
let querySetListSync connection read (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.querySetListSync
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read commandDefinition

/// Execute the command and return the 2 first sets of rows as a tuple of 2 lists accordingly to the command definition.
/// This function runs asynchronously.
let querySetList2 connection read1 read2 (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.querySetList2
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read1 read2 commandDefinition

/// Execute the command and return the 2 first sets of rows as a tuple of 2 lists accordingly to the command definition.
/// This function runs synchronously.
let querySetList2Sync connection read1 read2 (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.querySetList2Sync
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read1 read2 commandDefinition

/// Execute the command and return the 3 first sets of rows as a tuple of 3 lists accordingly to the command definition.
/// This function runs asynchronously.
let querySetList3 connection read1 read2 read3 (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.querySetList3
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read1 read2 read3 commandDefinition

/// Execute the command and return the 3 first sets of rows as a tuple of 3 lists accordingly to the command definition.
/// This function runs synchronously.
let querySetList3Sync  connection read1 read2 read3 (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.querySetList3Sync
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) read1 read2 read3 commandDefinition

/// Execute the command accordingly to its definition and,
/// - return the first cell value, if it is available and of the given type.
/// - throw an exception, otherwise.
/// This function runs asynchronously.
let executeScalar<'Scalar> connection (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.executeScalar<'Scalar, _, _, _, _, _, _, _, _>
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) commandDefinition

/// Execute the command accordingly to its definition and,
/// - return the first cell value, if it is available and of the given type.
/// - throw an exception, otherwise.
/// This function runs synchronously.
let executeScalarSync<'Scalar> connection (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.executeScalarSync<'Scalar, _, _, _, _, _, _, _, _>
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) commandDefinition

/// Execute the command accordingly to its definition and,
/// - return Some, if the first cell is available and of the given type.
/// - return None, if first cell is DBNull.
/// - throw an exception, otherwise.
/// This function runs asynchronously.
let executeScalarOrNone<'Scalar> connection (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.executeScalarOrNone<'Scalar, _, _, _, _, _, _, _, _>
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) commandDefinition

/// Execute the command accordingly to its definition and,
/// - return Some, if the first cell is available and of the given type.
/// - return None, if first cell is DBNull.
/// - throw an exception, otherwise.
/// This function runs synchronously.
let executeScalarOrNoneSync<'Scalar> connection (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.executeScalarOrNoneSync<'Scalar, _, _, _, _, _, _, _, _>
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) commandDefinition

/// Execute the command accordingly to its definition and, return the number of rows affected.
/// This function runs asynchronously.
let executeNonQuery connection (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.executeNonQuery
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) commandDefinition

/// Execute the command accordingly to its definition and, return the number of rows affected.
/// This function runs synchronously.
let executeNonQuerySync connection (commandDefinition: SqliteCommandDefinition) =
    SqlCommand.executeNonQuerySync
        connection (Constants.Deps) (SqliteConfiguration.Snapshot) commandDefinition
```

## 🦮 Null Helpers

We can create another module for null helpers, and the rest is all about passing the relevant parameters to the underlying core functions.

```fsharp
[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.Sqlite.SqliteNullDbValue

open Vp.FSharp.Sql


/// Return SQLite DB Null value if the given option is None, otherwise the underlying wrapped in Some.
let ifNone toDbValue = NullDbValue.ifNone toDbValue SqliteDbValue.Null

/// Return SQLite DB Null value if the option is Error, otherwise the underlying wrapped in Ok.
let ifError toDbValue = NullDbValue.ifError toDbValue (fun _ -> SqliteDbValue.Null)
```

## 🚄 Transaction Helpers

More of the same here too.

```fsharp
[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.Sqlite.SqliteTransaction

open Vp.FSharp.Sql
open Vp.FSharp.Sql.Sqlite


let private beginTransactionAsync = Constants.Deps.BeginTransactionAsync
let private beginTransaction = Constants.Deps.BeginTransaction

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// cancellation token and transaction body.
/// This function runs asynchronously.
let commit cancellationToken isolationLevel connection body =
    Transaction.commit cancellationToken isolationLevel connection beginTransactionAsync body

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// and transaction body.
/// This function runs synchronously.
let commitSync isolationLevel connection body =
    Transaction.commitSync isolationLevel connection beginTransaction body

/// Create and do not commit an automatically generated transaction with the given connection, isolation,
/// cancellation token and transaction body.
/// This function runs asynchronously.
let notCommit cancellationToken isolationLevel connection body =
    Transaction.notCommit cancellationToken isolationLevel connection beginTransactionAsync body

/// Create and do not commit an automatically generated transaction with the given connection, isolation,
/// and transaction body.
/// This function runs synchronously.
let notCommitSync isolationLevel connection body =
    Transaction.notCommitSync isolationLevel connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// cancellation token and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs asynchronously.
let commitOnSome cancellationToken isolationLevel connection body =
    Transaction.commitOnSome cancellationToken isolationLevel connection beginTransactionAsync body

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let commitOnSomeSync isolationLevel connection body =
    Transaction.commitOnSomeSync isolationLevel connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// cancellation token and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs asynchronously.
let commitOnOk cancellationToken isolationLevel connection body =
    Transaction.commitOnOk cancellationToken isolationLevel connection beginTransactionAsync body

/// Create and commit an automatically generated transaction with the given connection, isolation,
/// and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs synchronously.
let commitOnOkSync isolationLevel connection body =
    Transaction.commitOnOkSync isolationLevel connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// This function runs asynchronously.
let defaultCommit connection body = Transaction.defaultCommit connection beginTransactionAsync body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// This function runs synchronously.
let defaultCommitSync connection body = Transaction.defaultCommitSync connection beginTransaction body

/// Create and do not commit an automatically generated transaction with the given connection and transaction body.
/// This function runs asynchronously.
let defaultNotCommit connection body = Transaction.defaultNotCommit connection beginTransactionAsync body

/// Create and do not commit an automatically generated transaction with the given connection and transaction body.
/// This function runs synchronously.
let defaultNotCommitSync connection body = Transaction.defaultNotCommitSync connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs asynchronously.
let defaultCommitOnSome connection body = Transaction.defaultCommitOnSome connection beginTransactionAsync body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Ok.
/// This function runs synchronously.
let defaultCommitOnSomeSync connection body = Transaction.defaultCommitOnSomeSync connection beginTransaction body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs asynchronously.
let defaultCommitOnOk connection body = Transaction.defaultCommitOnOk connection beginTransactionAsync body

/// Create and commit an automatically generated transaction with the given connection and transaction body.
/// The commit phase only occurs if the transaction body returns Some.
/// This function runs synchronously.
let defaultCommitOnOkSync connection body = Transaction.defaultCommitOnOkSync connection beginTransaction body
```

![Congratulations!](https://media.giphy.com/media/TGcvcOiWBwvbsiTZjg/giphy.gif)

And voila! You're now all settled and ready to execute the wildest commands against your favorite database!

# 🌐 `TransactionScope` Helpers

These helpers work regardless of the ADO.NET provider you're using as long as it supports `TransactionScope`.

⚠ That being said, **we strongly discourage you from using those helpers**:
- 🚨 Bear in mind that [the support for distributed transactions is not yet available](https://github.com/dotnet/runtime/issues/715) since the .NET core era. 
- 🚨 Using `TransactionScope` (with or without those helpers) is very error-prone, and you might encounter unexpected behaviours without clear error messages.
- 🚨 Considering that there is very little evolution regarding this support, and therefore there is somehow limited applications to use the `TransactionScope` 
  without the support for distributed transactions, those helpers might move to a separate library (i.e. repository + nuget package).

If you need a viable workaround to 2PC or distributed transactions, you might want to check some architectural patterns such as [the Saga Pattern](https://www.youtube.com/watch?v=xDuwrtwYHu8).

# ❤ How to Contribute
Bug reports, feature requests, and pull requests are very welcome!

Please read the [Contribution Guidelines](./CONTRIBUTION.md) to get started.

# 📜 Licensing
The project is licensed under MIT. 

For more information on the license see the [license file](./LICENSE).

[sqlite-repo]: https://github.com/veepee-oss/Vp.FSharp.Sql.Sqlite
[sqlserver-repo]: https://github.com/veepee-oss/Vp.FSharp.Sql.SqlServer
[postgresql-repo]: https://github.com/veepee-oss/Vp.FSharp.Sql.PostgreSql
