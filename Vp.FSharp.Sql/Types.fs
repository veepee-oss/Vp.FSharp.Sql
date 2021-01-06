namespace Vp.FSharp.Sql

open System
open System.Data
open System.Data.Common
open System.Threading
open System.Threading.Tasks

open Vp.FSharp.Sql.Helpers


type Text =
    | Single of string
    | Multiple of string list

type SqlLog<'DbConnection, 'DbCommand
    when 'DbConnection :> DbConnection
    and 'DbCommand :> DbCommand> =
    | ConnectionOpened of connection: 'DbConnection
    | ConnectionClosed of connection: 'DbConnection * sinceOpened: TimeSpan
    | CommandPrepared of command: 'DbCommand
    | CommandExecuted of command: 'DbCommand * sincePrepared: TimeSpan

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

type SqlGlobalConf<'DbConnection, 'DbCommand
    when 'DbConnection :> DbConnection
    and 'DbCommand :> DbCommand> =
    { DefaultLogger: (SqlLog<'DbConnection, 'DbCommand> -> unit) option }

type SqlDeps<'DbConnection, 'DbTransaction, 'DbCommand, 'DbParameter, 'DbDataReader, 'DbType
    when 'DbConnection :> DbConnection
    and 'DbTransaction :> DbTransaction
    and 'DbCommand :> DbCommand
    and 'DbParameter :> DbParameter
    and 'DbDataReader :> DbDataReader> =
        { CreateCommand: 'DbConnection -> 'DbCommand
          ExecuteReaderAsync: 'DbCommand -> CancellationToken -> Task<'DbDataReader>
          DbValueToParameter: string -> 'DbType -> 'DbParameter }

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
