namespace Vp.FSharp.Sql

open System
open System.Data
open System.Threading
open System.Data.Common
open System.Threading.Tasks

open Vp.FSharp.Sql.Helpers


/// The type that represents the text of the command that is going to be run against the connection data source.
type Text =
    /// The text is represented as a single string.
    | Single of string
    /// The text is represented as multiple strings.
    | Multiple of string list

/// The type representing the different sql logs available.
type SqlLog<'DbConnection, 'DbCommand
    when 'DbConnection :> DbConnection
    and 'DbCommand :> DbCommand> =
    /// The connection has just been opened.
    | ConnectionOpened of connection: 'DbConnection
    /// The connection has just been closed.
    | ConnectionClosed of connection: 'DbConnection * sinceOpened: TimeSpan
    /// The command is just done being prepared and ready to be executed.
    | CommandPrepared of command: 'DbCommand
    /// The command is just done being executed.
    | CommandExecuted of command: 'DbCommand * sincePrepared: TimeSpan

/// The type representing the different kinds of logger available.
type LoggerKind<'DbConnection, 'DbCommand
    when 'DbConnection :> DbConnection
    and 'DbCommand :> DbCommand> =
    /// Default value: the one defined in the configuration, if any.
    | Configuration
    /// The default one is overriden and instead use this given value.
    | Override of (SqlLog<'DbConnection, 'DbCommand> -> unit)
    /// Nothing, ie. no logger assigned upon command execution.
    | Nothing

/// Contains the definition of a command upon its execution
type CommandDefinition<'DbConnection, 'DbCommand, 'DbParameter, 'DbDataReader, 'DbTransaction, 'DbType
    when 'DbConnection :> DbConnection
    and 'DbCommand :> DbCommand
    and 'DbParameter :> DbParameter
    and 'DbDataReader :> DbDataReader
    and 'DbTransaction :> DbTransaction> =
    { /// The text of the command that is going to be run against the connection data source.
      Text: Text

      /// The parameters of the SQL statement or stored procedure.
      Parameters: (string * 'DbType) list

      /// A cancellation token that can be used to request the operation to be cancelled early.
      CancellationToken: CancellationToken

      /// The wait time before terminating the attempt to execute a command and generating an error.
      Timeout: TimeSpan

      /// The way how the text is interpreted.
      CommandType: CommandType

      /// Indicates whether a prepared (or compiled) version of the command on the data source has to be done
      Prepare: bool

      /// The transactions within which the command is going to be executed.
      Transaction: 'DbTransaction option

      /// The logger to call upon events occurence.
      Logger: LoggerKind<'DbConnection, 'DbCommand> }

/// A data structure holding some configuration with the relevant generic constraints.
type SqlConfiguration<'DbConnection, 'DbCommand
        when 'DbConnection :> DbConnection
        and 'DbCommand :> DbCommand> =
    { DefaultLogger: (SqlLog<'DbConnection, 'DbCommand> -> unit) option }

/// The related module handling operations on configuration.
[<RequireQualifiedAccess>]
module SqlConfiguration =
    let internal defaultValue() = { DefaultLogger = None }

    /// Setting up the configuration
    let logger value (configuration: SqlConfiguration<'DbConnection, 'DbCommand>) =
        { configuration with DefaultLogger = Some value }

    /// Defines no logger for the given configuration.
    let noLogger (configuration: SqlConfiguration<'DbConnection, 'DbCommand>) =
        { configuration with DefaultLogger = None }

/// A configuration cache holding a single value per set of generic constraints
/// and giving an access to a snapshot at any given point in time.
/// Can serve and act as some sort of global configuration.
[<AbstractClass; Sealed>]
type SqlConfigurationCache<'DbConnection, 'DbCommand
        when 'DbConnection :> DbConnection
        and 'DbCommand :> DbCommand> private() =

    static let mutable instance: SqlConfiguration<'DbConnection, 'DbCommand> = SqlConfiguration.defaultValue()

    /// Get the current state of the configuration cache (i.e. SqlConfiguration is a record, and hence immutable)
    static member Snapshot with get () = instance

    /// Set up the logger callback
    static member Logger(value) = instance <- SqlConfiguration.logger value instance

    /// Set up no logger callback
    static member NoLogger() = instance <- SqlConfiguration.noLogger instance


// Ie. The ADO.NET Provider generic constraints mapper due to the lack of proper support for some variant of the SRTP
// and the hideous members shadowing occuring in most ADO.NET Providers implementation
type SqlDependencies<'DbConnection, 'DbCommand, 'DbParameter, 'DbDataReader, 'DbTransaction, 'DbType
    when 'DbConnection :> DbConnection
    and 'DbCommand :> DbCommand
    and 'DbParameter :> DbParameter
    and 'DbDataReader :> DbDataReader
    and 'DbTransaction :> DbTransaction> =
        { CreateCommand: 'DbConnection -> 'DbCommand
          SetCommandTransaction: 'DbCommand -> 'DbTransaction -> unit
          BeginTransaction: 'DbConnection -> IsolationLevel -> 'DbTransaction
          BeginTransactionAsync: 'DbConnection -> IsolationLevel -> CancellationToken -> ValueTask<'DbTransaction>
          ExecuteReader: 'DbCommand -> 'DbDataReader
          ExecuteReaderAsync: 'DbCommand -> CancellationToken -> Task<'DbDataReader>
          DbValueToParameter: string -> 'DbType -> 'DbParameter }

/// Represents a field collected by the SqlRecordReader
type DbField =
    { /// The field name as found in the result set.
      Name: string

      /// The field name as found in the result set.
      Index: int32

      /// The assigned .NET type assigned to this field.
      NetTypeName: string

      /// The field native type name as found in the result set.
      NativeTypeName: string }

// Wrap a specific DataReader
type SqlRecordReader<'DbDataReader when 'DbDataReader :> DbDataReader>(dataReader: 'DbDataReader) =
    let mapFieldIndex fieldIndex =
        { Index = fieldIndex
          Name = dataReader.GetName(fieldIndex)
          NetTypeName = dataReader.GetFieldType(fieldIndex).Name
          NativeTypeName = dataReader.GetDataTypeName(fieldIndex) }

    let cachedFields = [0 .. dataReader.FieldCount - 1] |> List.map mapFieldIndex
    let cachedFieldsByName = cachedFields |> List.map(fun field -> (field.Name, field)) |> readOnlyDict
    let cachedFieldsByIndex = cachedFields |> List.map(fun field -> (field.Index, field)) |> readOnlyDict

    let availableFields =
        cachedFieldsByName
        |> Seq.mapi (fun index kvp ->
            $"(%d{index})[%s{kvp.Key}:%s{kvp.Value.NetTypeName}|%s{kvp.Value.NativeTypeName}]")
        |> String.concat ", "

    let failToReadFieldByName fieldName fieldTypeName =
        failwithf $"""Could not read field '%s{fieldName}' as %s{fieldTypeName}.
                      Available fields are %s{availableFields}"""

    let failToReadFieldByIndex fieldIndex fieldTypeName =
        failwithf $"Could not read field at index %d{fieldIndex} as %s{fieldTypeName}. Available fields are %s{availableFields}"

    /// The current fields accessible by their resp. names
    member this.FieldsByName = cachedFieldsByName

    /// The current fields accessible by their resp. indexes
    member this.FieldsByIndex = cachedFieldsByIndex

    /// The current number of available fields
    member this.FieldCount = dataReader.FieldCount

    /// Get value of the given field via its name, if any, otherwise throw an exception.
    member this.Value<'T> (fieldName: string) =
        match cachedFieldsByName.TryGetValue(fieldName) with
            | true, column ->
                // https://github.com/npgsql/npgsql/issues/2087
                if dataReader.IsDBNull(column.Index) && DbNull.is<'T>() then DbNull.retypedAs<'T>()
                else dataReader.GetFieldValue<'T>(column.Index)
            | false, _ ->
                failToReadFieldByName fieldName typeof<'T>.Name

    /// Get value of the given field via its name:
    /// - return Some, if the value is available and of the given type.
    /// - return None, if the value is DBNull.
    /// - throw an exception, otherwise.
    member this.ValueOrNone<'T> (fieldName: string) =
        match cachedFieldsByName.TryGetValue(fieldName) with
        | true, column ->
            if dataReader.IsDBNull(column.Index) then None
            else Some (dataReader.GetFieldValue<'T>(column.Index))
        | false, _ ->
            failToReadFieldByName fieldName typeof<'T>.Name

    /// Get value of the given field via its index, if any, otherwise throw an exception.
    member this.Value<'T> (fieldIndex: int32) =
        match cachedFieldsByIndex.TryGetValue(fieldIndex) with
            | true, column ->
                // https://github.com/npgsql/npgsql/issues/2087
                if dataReader.IsDBNull(fieldIndex) && DbNull.is<'T>() then DbNull.retypedAs<'T>()
                else dataReader.GetFieldValue<'T>(column.Index)
            | false, _ ->
                failToReadFieldByIndex fieldIndex typeof<'T>.Name

    /// Get value of the given field via its index:
    /// - return Some, if the value is available and of the given type.
    /// - return None, if the value is DBNull.
    /// - throw an exception, otherwise.
    member this.ValueOrNone<'T> (columnIndex: int32) =
        match cachedFieldsByIndex.TryGetValue(columnIndex) with
        | true, column ->
            if dataReader.IsDBNull(column.Index) then None
            else Some (dataReader.GetFieldValue<'T>(column.Index))
        | false, _ ->
            failToReadFieldByIndex columnIndex typeof<'T>.Name

type SetNumber = int32
type RecordNumber = int32

type Read<'DbDataReader, 'T when 'DbDataReader :> DbDataReader> =
    SetNumber -> RecordNumber -> SqlRecordReader<'DbDataReader> -> 'T

type ReadSet<'DbDataReader, 'T when 'DbDataReader :> DbDataReader> =
    RecordNumber -> SqlRecordReader<'DbDataReader> -> 'T

exception SqlNoDataAvailableException
