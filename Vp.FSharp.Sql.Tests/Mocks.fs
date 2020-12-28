[<RequireQualifiedAccess>]
module internal Mocks

open System
open System.Data
open System.Data.Common

open Vp.FSharp.Sql


type Data = {
    Columns: string list
    Values: Object list list
}

let makeDependencies (valToParam: (string -> 'a -> 'b) option) logger = {
    CreateCommand = (fun connection -> connection.CreateCommand())
    ExecuteReaderAsync = (fun cmd -> cmd.ExecuteReaderAsync())
    DbValueToParameter = valToParam |> Option.defaultValue (fun _ _ -> failwith "")
    GlobalLogger = logger
}

let makeReader data _ =
    { new DbDataReader()
        with
        member this.Depth with get() = 0
        member this.FieldCount with get() = 0
        member this.HasRows with get() = true
        member this.IsClosed with get() = true
        member this.RecordsAffected with get() = 0
        member this.Item
            with get(ordinal: int):Object = null
        member this.Item
            with get(name: string):Object = null
        member this.GetDataTypeName (ordinal: int) = ""
        member this.GetEnumerator () = null
        member this.GetFieldType (ordinal: int) = null
        member this.GetName (ordinal: int) = ""
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
        member this.GetValue (ordinal: int) = null
        member this.GetValues values = 0
        member this.IsDBNull (ordinal: int) = false
        member this.NextResult () = true
        member this.Read () = true
    }

let makeCommand connection callBackReader =
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
            callBackReader commandBehavior
        member this.ExecuteNonQuery () = 0
        member this.ExecuteScalar () = null
        member this.Prepare () = ()
    }

let makeConnection cs state openCallback closeCallback makeReader =
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
        member this.CreateDbCommand () = makeCommand this makeReader
    }
