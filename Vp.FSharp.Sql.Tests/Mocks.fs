[<RequireQualifiedAccess>]
module internal Mocks

open System
open System.Data
open System.Data.Common

open System.Threading.Tasks

open Vp.FSharp.Sql


type DbField' =
    { Name: string
      FieldType: Type
      NativeTypeName: string }

type Data =
    { Columns: DbField' list list
      GetValues: int32 -> int32 -> obj list
      CountRows: int32 -> int32
      CountResultSets: int32 }

type Response =
    | Reader of (CommandBehavior -> DbDataReader)
    | NonQuery of int32

type IIsDisposed =
    inherit IDisposable
    abstract member IsDisposed : bool with get

[<AbstractClass>]
type IsDisposedDbConnection() =

    inherit DbConnection()
    let mutable isDisposed = false
    interface IIsDisposed with member this.IsDisposed = isDisposed
    member this.CheckDisposed(value) =
        if isDisposed then raise (ObjectDisposedException("Connection already disposed"))
        else value
    override this.Dispose(disposing) =
        this.CheckDisposed()
        base.Dispose(disposing)
        isDisposed <- true

[<AbstractClass>]
type IsDisposedDbCommand() =
    inherit DbCommand()

    let mutable isDisposed = false
    interface IIsDisposed with member this.IsDisposed = isDisposed
    member this.CheckDisposed(value) =
        if isDisposed then raise (ObjectDisposedException("Command already disposed"))
        else value
    override this.Dispose(disposing) =
        this.CheckDisposed()
        base.Dispose(disposing)
        isDisposed <- true

[<AbstractClass>]
type IsDisposedDbDataReader() =
    inherit DbDataReader()

    let mutable isDisposed = false
    interface IIsDisposed with member this.IsDisposed = isDisposed
    member this.CheckDisposed(value) =
        if isDisposed then raise (ObjectDisposedException("Data Reader already disposed"))
        else value
    override this.Dispose(disposing) =
        this.CheckDisposed()
        base.Dispose(disposing)
        isDisposed <- true

let fakeData values columns =
    { Columns = columns
      GetValues =
        fun resultSetIndex rowIndex ->
            match (resultSetIndex, rowIndex) with
            | resultSetIndex, rowIndex
                when resultSetIndex >= 0
                     && resultSetIndex < List.length values
                     && rowIndex >= 0
                     && rowIndex < List.length values.[resultSetIndex]
                -> values.[resultSetIndex].[rowIndex]
            | _ -> $"get values: out of resultSetIndex %i{resultSetIndex} or rowIndex %i{rowIndex}" |> failwith
      CountRows =
        fun resultSetIndex ->
            match resultSetIndex with
            | resultSetIndex when resultSetIndex >= 0 && resultSetIndex < List.length values
                -> List.length values.[resultSetIndex]
            | _ -> $"count rows: out of resultSetIndex %i{resultSetIndex}" |> failwith
      CountResultSets = List.length values }

let makeDeps (valToParam: (string -> 'DbType -> 'DbParameter) option)  =
    { CreateCommand = (fun connection -> connection.CreateCommand())
      SetCommandTransaction = (fun command transaction -> command.Transaction <- transaction)
      BeginTransaction = (fun _ _ -> null)
      BeginTransactionAsync = (fun _ _ _ -> ValueTask.FromResult null)
      ExecuteReader = (fun cmd -> cmd.ExecuteReader())
      ExecuteReaderAsync = (fun cmd -> cmd.ExecuteReaderAsync)
      DbValueToParameter = valToParam |> Option.defaultValue (fun _ _ -> failwith "") }

let makeConf logger =
    { DefaultLogger = logger }

let makeReader data _ =
    let mutable currentRowIndex = -1
    let mutable currentResultSetIndex = 0
    { new IsDisposedDbDataReader()
        with
        member this.Depth with get () = this.CheckDisposed(0)
        member this.FieldCount with get () =
            List.length data.Columns.[currentResultSetIndex]
            |> this.CheckDisposed
        member this.HasRows with get () =
            data.CountRows currentResultSetIndex > 0
            |> this.CheckDisposed
        member this.IsClosed with get () = this.CheckDisposed(true)
        member this.RecordsAffected with get () = this.CheckDisposed(0)
        member this.Item with get (ordinal: int32): obj = this.CheckDisposed(null)
        member this.Item with get (name: string): obj = this.CheckDisposed(null)
        member this.GetDataTypeName (ordinal: int32) =
            data.Columns.[currentResultSetIndex].[ordinal].NativeTypeName
            |> this.CheckDisposed
        member this.GetEnumerator () = this.CheckDisposed(null)
        member this.GetFieldType (ordinal: int32) =
            data.Columns.[currentResultSetIndex].[ordinal].FieldType
            |> this.CheckDisposed
        member this.GetName (ordinal: int32) =
            data.Columns.[currentResultSetIndex].[ordinal].Name
            |> this.CheckDisposed
        member this.GetOrdinal (name: string) = this.CheckDisposed(0)
        member this.GetBoolean (ordinal: int32) = this.CheckDisposed(true)
        member this.GetByte (ordinal: int32) = this.CheckDisposed(0uy)
        member this.GetBytes (ordinal, dataOffset, buffer, bufferOffset, length) = this.CheckDisposed(0L)
        member this.GetChar (ordinal: int32) = this.CheckDisposed('a')
        member this.GetChars (ordinal, dataOffset, buffer, bufferOffset, length) = this.CheckDisposed(0L)
        member this.GetDateTime (ordinal: int32) = this.CheckDisposed(DateTime.Today)
        member this.GetDecimal (ordinal: int32) = this.CheckDisposed(0M)
        member this.GetDouble (ordinal: int32) = this.CheckDisposed(0.0)
        member this.GetFloat (ordinal: int32) = this.CheckDisposed(0F)
        member this.GetGuid (ordinal: int32) = this.CheckDisposed(Guid.Empty)
        member this.GetInt16 (ordinal: int32) = this.CheckDisposed(0s)
        member this.GetInt32 (ordinal: int32) = this.CheckDisposed(0)
        member this.GetInt64 (ordinal: int32) = this.CheckDisposed(0L)
        member this.GetString (ordinal: int32) = this.CheckDisposed("")
        member this.GetValue (ordinal: int32) =
            (data.GetValues currentResultSetIndex currentRowIndex).[ordinal]
            |> this.CheckDisposed
        member this.GetValues values = this.CheckDisposed(0)
        member this.IsDBNull (ordinal: int32) =
            (data.GetValues currentResultSetIndex currentRowIndex).[ordinal] = null
            |> this.CheckDisposed
        member this.NextResult () =
            currentResultSetIndex <- currentResultSetIndex + 1
            currentRowIndex <- -1
            currentResultSetIndex < data.CountResultSets
            |> this.CheckDisposed
        member this.Read () =
            currentRowIndex <- currentRowIndex + 1
            currentRowIndex < data.CountRows currentResultSetIndex
            |> this.CheckDisposed
    } :> DbDataReader

let makeCommand connection response =
    let mutable connection = connection
    let mutable cmdTxt = ""
    { new IsDisposedDbCommand()
        with
        member this.CommandText
            with get () = this.CheckDisposed(cmdTxt)
            and  set v  = this.CheckDisposed(cmdTxt <- v)
        member this.CommandTimeout
            with get () = this.CheckDisposed(0)
            and  set v  = this.CheckDisposed()
        member this.CommandType
            with get () = this.CheckDisposed(CommandType.Text)
            and  set v = ()
        member this.DbConnection
            with get () = this.CheckDisposed(connection)
            and  set v  = this.CheckDisposed(connection <- v)
        member this.DbParameterCollection
            with get() = this.CheckDisposed(null)
        member this.DbTransaction
            with get () = this.CheckDisposed(null)
            and  set v  = this.CheckDisposed()
        member this.DesignTimeVisible
            with get () = this.CheckDisposed(false)
            and  set v  = this.CheckDisposed()
        member this.UpdatedRowSource
            with get () = this.CheckDisposed(UpdateRowSource())
            and  set v  = this.CheckDisposed()
        member this.Cancel () = this.CheckDisposed()
        member this.CreateDbParameter () = this.CheckDisposed(null)
        member this.ExecuteDbDataReader commandBehavior =
            match response with
            | Reader callBackReader -> callBackReader commandBehavior
            | _ -> failwith "ExecuteDbDataReader"
            |> this.CheckDisposed
        member this.ExecuteNonQuery () =
            match response with
            | NonQuery response -> response
            | _ -> failwith "ExecuteNonQuery"
            |> this.CheckDisposed
        member this.ExecuteScalar () = this.CheckDisposed(null)
        member this.Prepare () = this.CheckDisposed()
    } :> DbCommand

let makeConnectionReader cs state openCallback closeCallback response =
    let mutable connectionString = cs
    { new IsDisposedDbConnection()
        with
        member this.ConnectionString
            with get () = this.CheckDisposed(connectionString)
            and  set v  = this.CheckDisposed(connectionString <- v)
        member this.Database with get () = this.CheckDisposed("")
        member this.DataSource with get () = this.CheckDisposed("")
        member this.ServerVersion with get () = this.CheckDisposed("")
        member this.State with get () = this.CheckDisposed(state)
        member this.Close () =
            closeCallback ()
            |> this.CheckDisposed
        member this.ChangeDatabase dbName = this.CheckDisposed()
        member this.Open() =
            openCallback ()
            |> this.CheckDisposed
        member this.BeginDbTransaction isolationLevel =
            failwith "BeginDbTransaction not called"
            |> this.CheckDisposed

        member this.CreateDbCommand () =
            makeCommand this response
            |> this.CheckDisposed
    } :> DbConnection

let makeConnection cs state openCallback closeCallback response =
    let mutable connectionString = cs
    { new IsDisposedDbConnection()
        with
        member this.ConnectionString
            with get () = this.CheckDisposed(connectionString)
            and  set v  = this.CheckDisposed(connectionString <- v)
        member this.Database with get () = this.CheckDisposed("")
        member this.DataSource with get () = this.CheckDisposed("")
        member this.ServerVersion with get () = this.CheckDisposed("")
        member this.State with get () = this.CheckDisposed(state)
        member this.Close () =
            closeCallback ()
            |> this.CheckDisposed
        member this.ChangeDatabase dbName = this.CheckDisposed()
        member this.Open() =
            openCallback ()
            |> this.CheckDisposed
        member this.BeginDbTransaction isolationLevel =
            failwith "BeginDbTransaction not called"
            |> this.CheckDisposed
        member this.CreateDbCommand () =
            makeCommand this response
            |> this.CheckDisposed
    } :> DbConnection
