[<RequireQualifiedAccess>]
module internal Mocks

open System
open System.Data
open System.Data.Common

open Vp.FSharp.Sql


type DbField' =
    { Name: string
      FieldType: Type
      NativeTypeName: string }

type Data =
    { Columns: DbField' list list
      GetValues: int32 -> int32 -> Object list
      CountRows: int32 -> int32
      CountResultSets: int32 }

type Response =
    | Reader of (CommandBehavior -> DbDataReader)
    | NonQuery of int32

let fakeData values columns =
    { Columns = columns
      GetValues =
        fun resultSetIndex rowIndex ->
            match (resultSetIndex, rowIndex) with
            | (resultSetIndex, rowIndex)
                when resultSetIndex >= 0
                     && resultSetIndex < List.length values
                     && rowIndex >= 0
                     && rowIndex < List.length values.[resultSetIndex]
                -> values.[resultSetIndex].[rowIndex]
            | _ -> sprintf "get values: out of resultSetIndex %i or rowIndex %i" resultSetIndex rowIndex |> failwith
      CountRows =
        fun resultSetIndex ->
            match resultSetIndex with
            | resultSetIndex when resultSetIndex >= 0 && resultSetIndex < List.length values
                -> List.length values.[resultSetIndex]
            | _ -> sprintf "count rows: out of resultSetIndex %i" resultSetIndex |> failwith
      CountResultSets = List.length values }

let makeDependencies (valToParam: (string -> 'a -> 'b) option) logger =
    { CreateCommand = (fun connection -> connection.CreateCommand())
      ExecuteReaderAsync = (fun cmd -> cmd.ExecuteReaderAsync())
      DbValueToParameter = valToParam |> Option.defaultValue (fun _ _ -> failwith "")
      GlobalLogger = logger }

let makeReader data _ =
    let mutable currentRowIndex = -1
    let mutable currentResultSetIndex = 0
    { new DbDataReader()
        with
        member this.Depth with get() = 0
        member this.FieldCount with get() =
            List.length data.Columns.[currentResultSetIndex]
        member this.HasRows with get() =
            data.CountRows currentResultSetIndex > 0
        member this.IsClosed with get() = true
        member this.RecordsAffected with get() = 0
        member this.Item
            with get(ordinal: int):Object = null
        member this.Item
            with get(name: string):Object = null
        member this.GetDataTypeName (ordinal: int) =
            data.Columns.[currentResultSetIndex].[ordinal].NativeTypeName
        member this.GetEnumerator () = null
        member this.GetFieldType (ordinal: int) =
            data.Columns.[currentResultSetIndex].[ordinal].FieldType
        member this.GetName (ordinal: int) =
            data.Columns.[currentResultSetIndex].[ordinal].Name
        member this.GetOrdinal (name: string) = 0
        member this.GetBoolean (ordinal: int) = true
        member this.GetByte (ordinal: int) = 0uy
        member this.GetBytes (ordinal, dataOffset, buffer, bufferOffset, length) = 0L
        member this.GetChar (ordinal: int) = 'a'
        member this.GetChars (ordinal, dataOffset, buffer, bufferOffset, length) = 0L
        member this.GetDateTime (ordinal: int) = DateTime.Today
        member this.GetDecimal (ordinal: int) = 0M
        member this.GetDouble (ordinal: int) = 0.0
        member this.GetFloat (ordinal: int) = 0F
        member this.GetGuid (ordinal: int) = Guid.Empty
        member this.GetInt16 (ordinal: int) = 0s
        member this.GetInt32 (ordinal: int) = 0
        member this.GetInt64 (ordinal: int) = 0L
        member this.GetString (ordinal: int) = ""
        member this.GetValue (ordinal: int) =
            (data.GetValues currentResultSetIndex currentRowIndex).[ordinal]
        member this.GetValues values = 0
        member this.IsDBNull (ordinal: int) =
            (data.GetValues currentResultSetIndex currentRowIndex).[ordinal] = null
        member this.NextResult () =
            currentResultSetIndex <- currentResultSetIndex + 1
            currentRowIndex <- -1
            currentResultSetIndex < data.CountResultSets
        member this.Read () =
            currentRowIndex <- currentRowIndex + 1
            currentRowIndex < data.CountRows currentResultSetIndex
    }

let makeCommand connection response =
    let mutable connection = connection
    let mutable cmdTxt = ""
    { new DbCommand()
        with
        member this.CommandText
            with get() = cmdTxt
            and set(v) = cmdTxt <- v
        member this.CommandTimeout
            with get() = 0
            and set(v) = ()
        member this.CommandType
            with get() = CommandType.Text
            and set(v) = ()
        member this.DbConnection
            with get() = connection
            and set(v) = connection <- v
        member this.DbParameterCollection
            with get() = null
        member this.DbTransaction
            with get() = null
            and set(v) = ()
        member this.DesignTimeVisible
            with get() = false
            and set(v) = ()
        member this.UpdatedRowSource
            with get() = UpdateRowSource()
            and set(v) = ()
        member this.Cancel () = ()
        member this.CreateDbParameter () = null
        member this.ExecuteDbDataReader commandBehavior =
            match response with
            | Reader callBackReader -> callBackReader commandBehavior
            | _ -> failwith "ExecuteDbDataReader"
        member this.ExecuteNonQuery () =
            match response with
            | NonQuery response -> response
            | _ -> failwith "ExecuteNonQuery"
        member this.ExecuteScalar () = null
        member this.Prepare () = ()
    }

let makeConnectionReader cs state openCallback closeCallback response =
    let mutable connectionString = cs
    { new DbConnection()
        with
        member this.ConnectionString
            with get() = connectionString
            and set(v) = connectionString <- v
        member this.Database
            with get() = ""
        member this.DataSource
            with get() = ""
        member this.ServerVersion
            with get() = ""
        member this.State
            with get() = state
        member this.Close () = closeCallback ()
        member this.ChangeDatabase dbName = ()
        member this.Open() = openCallback ()
        member this.BeginDbTransaction isolationLevel = failwith "BeginDbTransaction not called"
        member this.CreateDbCommand () = makeCommand this response
    }

let makeConnection cs state openCallback closeCallback response =
    let mutable connectionString = cs
    { new DbConnection()
        with
        member this.ConnectionString
            with get() = connectionString
            and set(v) = connectionString <- v
        member this.Database
            with get() = ""
        member this.DataSource
            with get() = ""
        member this.ServerVersion
            with get() = ""
        member this.State
            with get() = state
        member this.Close () = closeCallback ()
        member this.ChangeDatabase dbName = ()
        member this.Open() = openCallback ()
        member this.BeginDbTransaction isolationLevel = failwith "BeginDbTransaction not called"
        member this.CreateDbCommand () = makeCommand this response
    }
