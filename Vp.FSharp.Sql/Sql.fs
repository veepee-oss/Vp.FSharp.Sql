namespace Vp.FSharp.Sql

open System
open System.Data
open System.Threading
open System.Data.Common

open System.Threading.Tasks

open FSharp.Control

open Vp.FSharp.Sql.Helpers


type Text =
    | Single of string
    | Multiple of string list

type SqlLog<'DbConnection, 'DbCommand
    when 'DbConnection :> DbConnection
    and 'DbCommand :> DbCommand> =
    | ConnectionOpened of connection: 'DbConnection
    | ConnectionClosed of connection: 'DbConnection
    | CommandPrepared of command: 'DbCommand
    | CommandExecuted of command: 'DbCommand * sinceOpened: TimeSpan

type LoggerKind<'DbConnection, 'DbCommand
    when 'DbConnection :> DbConnection
    and 'DbCommand :> DbCommand> =
    | Global
    | Override of (SqlLog<'DbConnection, 'DbCommand> -> unit)
    | Nothing

type CommandDefinition<'DbConnection, 'DbTransaction, 'DbCommand, 'DbParameter, 'DbDataReader, 'DbType
    when 'DbConnection :> DbConnection
    and 'DbTransaction :> DbTransaction
    and 'DbCommand :> DbCommand
    and 'DbParameter :> DbParameter
    and 'DbDataReader :> DbDataReader> =
    { Text: Text
      Parameters: (string * 'DbType) list
      CancellationToken: CancellationToken
      Timeout: TimeSpan
      CommandType: CommandType
      Prepare: bool
      Transaction: 'DbTransaction option
      Logger: LoggerKind<'DbConnection, 'DbCommand> }

type SqlDeps<'DbConnection, 'DbTransaction, 'DbCommand, 'DbParameter, 'DbDataReader, 'DbType
    when 'DbConnection :> DbConnection
    and 'DbTransaction :> DbTransaction
    and 'DbCommand :> DbCommand
    and 'DbParameter :> DbParameter
    and 'DbDataReader :> DbDataReader> = {
        CreateCommand: 'DbConnection -> 'DbCommand
        ExecuteReaderAsync: 'DbCommand -> Task<'DbDataReader>
        DbValueToParameter: string -> 'DbType -> 'DbParameter
        GlobalLogger: (SqlLog<'DbConnection, 'DbCommand> -> unit) option
    }

type DbField =
    { Name: string
      Index: int32
      NetTypeName: string
      NativeTypeName: string }

type SqlRecordReader<'DbDataReader when 'DbDataReader :> DbDataReader>(dataReader: 'DbDataReader) =
    let mapFieldIndex fieldIndex =
        { Index = fieldIndex
          Name = dataReader.GetName(fieldIndex)
          NetTypeName = dataReader.GetFieldType(fieldIndex).Name
          NativeTypeName = dataReader.GetDataTypeName(fieldIndex) }

    let cachedFields = [0 .. dataReader.FieldCount - 1] |> List.map mapFieldIndex
    let cachedFieldsByName = cachedFields |> List.map(fun column -> (column.Name, column)) |> readOnlyDict
    let cachedFieldsByIndex = cachedFields |> List.map(fun column -> (column.Index, column)) |> readOnlyDict

    let availableFields =
        cachedFieldsByName
        |> Seq.mapi (fun index kvp ->
            sprintf "(%d)[%s:%s|%s]" index kvp.Key kvp.Value.NetTypeName kvp.Value.NativeTypeName)
        |> String.concat ", "

    let failToReadColumnByName columnName columnTypeName =
        failwithf "Could not read field '%s' as %s. Available fields are %s"
            columnName columnTypeName availableFields

    let failToReadColumnByIndex columnIndex columnTypeName =
        failwithf "Could not read field at index %d as %s. Available fields are %s"
            columnIndex columnTypeName availableFields

    member this.ColumnsByName = cachedFieldsByName
    member this.ColumnsByIndex = cachedFieldsByIndex
    member this.Count = dataReader.FieldCount

    member this.Value<'T> (columnName: string) =
        match cachedFieldsByName.TryGetValue(columnName) with
            | true, column ->
                // https://github.com/npgsql/npgsql/issues/2087
                if dataReader.IsDBNull(columnName) && DbNull.is<'T>() then DbNull.retypedAs<'T>()
                else dataReader.GetFieldValue<'T>(column.Index)
            | false, _ ->
                failToReadColumnByName columnName typeof<'T>.Name

    member this.ValueOrNone<'T> (columnName: string) =
        match cachedFieldsByName.TryGetValue(columnName) with
        | true, column ->
            if dataReader.IsDBNull(column.Index) then None
            else Some (dataReader.GetFieldValue<'T>(column.Index))
        | false, _ ->
            failToReadColumnByName columnName typeof<'T>.Name

    member this.Value<'T> (columnIndex: int32) =
        match cachedFieldsByIndex.TryGetValue(columnIndex) with
            | true, column ->
                // https://github.com/npgsql/npgsql/issues/2087
                if dataReader.IsDBNull(columnIndex) && DbNull.is<'T>() then DbNull.retypedAs<'T>()
                else dataReader.GetFieldValue<'T>(column.Index)
            | false, _ ->
                failToReadColumnByIndex columnIndex typeof<'T>.Name

    member this.ValueOrNone<'T> (columnIndex: int32) =
        match cachedFieldsByIndex.TryGetValue(columnIndex) with
        | true, column ->
            if dataReader.IsDBNull(column.Index) then None
            else Some (dataReader.GetFieldValue<'T>(column.Index))
        | false, _ ->
            failToReadColumnByIndex columnIndex typeof<'T>.Name

exception SqlNoDataAvailableException


[<RequireQualifiedAccess>]
module SqlCommand =

    [<Literal>]
    let DefaultTimeoutInSeconds = 30.

    [<Literal>]
    let DefaultPrepare = false

    [<Literal>]
    let DefaultCommandType = CommandType.Text

    let private defaultCommandDefinition() =
        { Text = Text.Single String.Empty
          Parameters = []
          CancellationToken = CancellationToken.None
          Timeout = TimeSpan.FromSeconds(DefaultTimeoutInSeconds)
          CommandType = DefaultCommandType
          Prepare = DefaultPrepare
          Transaction = None
          Logger = Nothing }

    /// Initialize a command definition with the given text contained in the given string.
    let text value = { defaultCommandDefinition() with Text = Text.Single value }

    /// Initialize a command definition with the given text spanning over several strings (ie. list).
    let textFromList value = { defaultCommandDefinition() with Text = Text.Multiple value }

    /// Update the command definition so that when executing the command, it doesn't use any logger.
    /// Be it the default one (Global, if any.) or a previously overriden one.
    let noLogger commandDefinition = { commandDefinition with Logger = LoggerKind.Nothing }

    /// Update the command definition so that when executing the command, it use the given overriding logger.
    /// instead of the default one, aka the Global logger, if any.
    let overrideLogger value commandDefinition = { commandDefinition with Logger = LoggerKind.Override value }

    /// Update the command definition with the given parameters.
    let parameters value commandDefinition = { commandDefinition with Parameters = value }

    /// Update the command definition with the given cancellation token.
    let cancellationToken value commandDefinition = { commandDefinition with CancellationToken = value }

    /// Update the command definition with the given timeout.
    let timeout value commandDefinition = { commandDefinition with Timeout = value }

    /// Update the command definition and sets the command type (ie. how it should be interpreted).
    let commandType value commandDefinition = { commandDefinition with CommandType = value }

    /// Update the command definition and sets whether the command should be prepared or not.
    let prepare value commandDefinition = { commandDefinition with Prepare = value }

    /// Update the command definition and sets whether the command should be wrapped in the given transaction.
    let transaction value commandDefinition = { commandDefinition with Transaction = Some value }

    let private formatParameterName (parameterName: string) =
        if not (parameterName.StartsWith "@") then sprintf "@%s" parameterName
        else parameterName

    let private setupCommand deps commandDefinition cancellationToken connection =
        async {
            let command = deps.CreateCommand connection

            Option.map
                (fun transaction -> command.Transaction <- transaction)
                (commandDefinition.Transaction)
            |> ignore

            match commandDefinition.Text with
            | Single value -> command.CommandText <- String.trimLeft value
            | Multiple value -> command.CommandText <- String.stitch value

            commandDefinition.Parameters
            |> List.iter(fun (name, value) ->
                let parameter = deps.DbValueToParameter name value
                command.Parameters.Add(parameter) |> ignore)

            command.CommandTimeout <- int32 commandDefinition.Timeout.TotalMilliseconds
            command.CommandType <- commandDefinition.CommandType

            if commandDefinition.Prepare then
                do! command.PrepareAsync(cancellationToken) |> Async.AwaitTask

            return command
        }

    let private setupConnection (connection: #DbConnection) cancellationToken =
        connection.OpenAsync(cancellationToken)
        |> Async.AwaitTask

    type private ReadState = { Continue: bool; SetIndex: int32; RecordIndex: int32 }

    let private readNextResultRecord state (dataReader: #DbDataReader) cancellationToken =
        async {
            let! nextRecordOk = dataReader.ReadAsync(cancellationToken) |> Async.AwaitTask
            if nextRecordOk then
                return { state with RecordIndex = state.RecordIndex + 1 }
            else
                let! nextResultSetOk = dataReader.NextResultAsync(cancellationToken) |> Async.AwaitTask
                if nextResultSetOk then
                    let! firstNextResultSetRecordOk = dataReader.ReadAsync(cancellationToken) |> Async.AwaitTask
                    if firstNextResultSetRecordOk then
                        return { state with SetIndex = state.SetIndex + 1; RecordIndex = 0 }
                    else
                        return { state with Continue = false }
                else
                    return { state with Continue = false }
        }

    let private log deps commandDefinition sqlLog =
        match commandDefinition.Logger with
        | Global -> deps.GlobalLogger
        | Override logging -> Some logging
        | Nothing -> None
        |> Option.map (fun callback -> sqlLog |> callback)
        |> ignore

    /// Return the sets of rows as an AsyncSeq accordingly to the command definition.
    let queryAsyncSeq (connection: #DbConnection) deps read commandDefinition =
        asyncSeq {
            let! linkedToken = Async.linkedTokenSourceFrom commandDefinition.CancellationToken
            let wasClosed = connection.State = ConnectionState.Closed
            let log sqlLog = log deps commandDefinition sqlLog
            let sw = System.Diagnostics.Stopwatch()
            use! command = setupCommand deps commandDefinition linkedToken connection

            try
                if wasClosed then
                    do! setupConnection connection linkedToken
                    ConnectionOpened connection |> log

                CommandPrepared command |> log
                sw.Start ()
                use! dbDataReader = command.ExecuteReaderAsync(linkedToken) |> Async.AwaitTask
                let items =
                    AsyncSeq.initInfinite(fun _ -> (dbDataReader, linkedToken))
                    |> AsyncSeq.scanAsync(
                        fun state (dataReader, cancellationToken) ->
                            readNextResultRecord state dataReader cancellationToken )
                            { Continue = true; SetIndex = 0; RecordIndex = -1 }
                    |> AsyncSeq.skip(1)
                    |> AsyncSeq.takeWhile(fun state -> state.Continue)
                    |> AsyncSeq.mapChange(fun state -> state.SetIndex) (fun _ -> SqlRecordReader(dbDataReader))
                    |> AsyncSeq.mapAsync(fun (state, rowReader) -> async {
                        return read state.SetIndex state.RecordIndex rowReader
                    })
                yield! items

            finally
                sw.Stop ()
                CommandExecuted (command, sw.Elapsed) |> log
                if wasClosed then
                    connection.Close()
                    ConnectionClosed connection |> log
        }

    /// Return the sets of rows as a list accordingly to the command definition.
    let queryList connection dbValueToParameter read commandDefinition =
        queryAsyncSeq connection dbValueToParameter read commandDefinition
        |> AsyncSeq.toListAsync

    /// Return the first set of rows as a list accordingly to the command definition.
    let querySetList (connection: #DbConnection) deps read commandDefinition =
        async {
            let setList = ResizeArray()
            let readRecord setIndex recordIndex recordReader =
                if setIndex = 0 then setList.Add(read recordIndex recordReader)
                else ()
            do! queryAsyncSeq connection deps readRecord commandDefinition |> AsyncSeq.consume
            return setList |> Seq.toList
        }

    /// Return the 2 first sets of rows as a tuple of 2 lists accordingly to the command definition.
    let querySetList2 connection deps read1 read2 commandDefinition =
        async {
            let set1List = ResizeArray()
            let set2List = ResizeArray()
            let readRecord setIndex recordIndex recordReader =
                if   setIndex = 0 then set1List.Add(read1 recordIndex recordReader)
                elif setIndex = 1 then set2List.Add(read2 recordIndex recordReader)
                else ()
            do! queryAsyncSeq connection deps readRecord commandDefinition |> AsyncSeq.consume
            return (set1List |> Seq.toList, set2List |> Seq.toList)
        }

    /// Return the 3 first sets of rows as a tuple of 3 lists accordingly to the command definition.
    let querySetList3 connection deps read1 read2 read3 commandDefinition =
        async {
            let set1List = ResizeArray()
            let set2List = ResizeArray()
            let set3List = ResizeArray()
            let readRecord setIndex recordIndex recordReader =
                if   setIndex = 0 then set1List.Add(read1 recordIndex recordReader)
                elif setIndex = 1 then set2List.Add(read2 recordIndex recordReader)
                elif setIndex = 2 then set3List.Add(read3 recordIndex recordReader)
                else ()
            do! queryAsyncSeq connection deps readRecord commandDefinition |> AsyncSeq.consume
            return (set1List |> Seq.toList, set2List |> Seq.toList, set3List |> Seq.toList)
        }

    /// Execute the command accordingly to its definition and,
    /// - return the first cell value, if it is available and of the given type.
    /// - throw an exception, otherwise.
    let executeScalar<'Scalar, .. > (connection: #DbConnection) deps commandDefinition =
        async {
            let! linkedToken = Async.linkedTokenSourceFrom commandDefinition.CancellationToken
            let wasClosed = connection.State = ConnectionState.Closed
            let log sqlLog = log deps commandDefinition sqlLog
            let sw = System.Diagnostics.Stopwatch()
            use! command = setupCommand deps commandDefinition linkedToken connection

            try
                if wasClosed then
                    do! setupConnection connection linkedToken
                    ConnectionOpened connection |> log

                CommandPrepared command |> log
                sw.Start ()
                use! dataReader = command.ExecuteReaderAsync(linkedToken) |> Async.AwaitTask
                let! anyData = dataReader.ReadAsync(linkedToken) |> Async.AwaitTask
                if not anyData then
                    return raise SqlNoDataAvailableException
                else
                    // https://github.com/npgsql/npgsql/issues/2087
                    if dataReader.IsDBNull(0) && DbNull.is<'Scalar>() then
                        return DbNull.retypedAs<'Scalar>()
                    else
                        return dataReader.GetFieldValue<'Scalar>(0)
            finally
                sw.Stop ()
                CommandExecuted (command, sw.Elapsed) |> log
                if wasClosed then
                    connection.Close()
                    ConnectionClosed connection |> log
        }

    /// Execute the command accordingly to its definition and,
    /// - return Some, if the first cell is available and of the given type.
    /// - return None, if first cell is DbNull.
    /// - throw an exception, otherwise.
    let executeScalarOrNone<'Scalar, .. > (connection: #DbConnection) deps commandDefinition =
        async {

            let! linkedToken = Async.linkedTokenSourceFrom commandDefinition.CancellationToken
            let wasClosed = connection.State = ConnectionState.Closed
            let log sqlLog = log deps commandDefinition sqlLog
            let sw = System.Diagnostics.Stopwatch()
            use! command = setupCommand deps commandDefinition linkedToken connection

            try
                if wasClosed then
                    do! setupConnection connection linkedToken
                    ConnectionOpened connection |> log

                CommandPrepared command |> log
                sw.Start ()
                use! dataReader = command.ExecuteReaderAsync(linkedToken) |> Async.AwaitTask
                let! anyData = dataReader.ReadAsync(linkedToken) |> Async.AwaitTask
                if not anyData then
                    return raise SqlNoDataAvailableException
                else
                    if dataReader.IsDBNull(0) then return None
                    else return Some (dataReader.GetFieldValue<'Scalar>(0))
            finally
                sw.Stop ()
                CommandExecuted (command, sw.Elapsed) |> log
                if wasClosed then
                    connection.Close()
                    ConnectionClosed connection |> log
        }

    /// Execute the command accordingly to its definition and, return the number of rows affected.
    let executeNonQuery (connection: #DbConnection) deps commandDefinition =
        async {
            let! linkedToken = Async.linkedTokenSourceFrom commandDefinition.CancellationToken
            let wasClosed = connection.State = ConnectionState.Closed
            let log sqlLog = log deps commandDefinition sqlLog
            let sw = System.Diagnostics.Stopwatch()
            use! command = setupCommand deps commandDefinition linkedToken connection

            try
                if wasClosed then
                    do! setupConnection connection linkedToken
                    ConnectionOpened connection |> log

                CommandPrepared command |> log
                sw.Start ()
                return! command.ExecuteNonQueryAsync(linkedToken) |> Async.AwaitTask
            finally
                sw.Stop ()
                CommandExecuted (command, sw.Elapsed) |> log
                if wasClosed then
                    connection.Close()
                    ConnectionClosed connection |> log
        }
