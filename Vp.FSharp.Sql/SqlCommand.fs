[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.SqlCommand

    open System
    open System.Data
    open System.Data.Common
    open System.Diagnostics
    open System.Threading

    open FSharp.Control

    open Vp.FSharp.Sql.Helpers


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
          Logger = LoggerKind.Configuration }

    /// Initialize a new command definition with the given text contained in the given string.
    let text value = { defaultCommandDefinition() with Text = Text.Single value }

    /// Initialize a new command definition with the given text spanning over several strings (ie. list).
    let textFromList value =
        { defaultCommandDefinition() with Text = Text.Multiple value }

    /// Update the command definition so that when executing the command, it doesn't use any logger.
    /// Be it the default one (Global, if any.) or a previously overriden one.
    let noLogger commandDefinition =
        { commandDefinition with Logger = Nothing }

    /// Update the command definition so that when executing the command, it use the given overriding logger.
    /// instead of the default one, aka the Global logger, if any.
    let overrideLogger value commandDefinition =
        { commandDefinition with Logger = LoggerKind.Override value }

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

            Option.iter
                (deps.SetCommandTransaction command)
                (commandDefinition.Transaction)

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

    let private setupCommandSync deps commandDefinition connection =
        let command = deps.CreateCommand connection

        Option.iter
            (deps.SetCommandTransaction command)
            (commandDefinition.Transaction)

        match commandDefinition.Text with
        | Single value -> command.CommandText <- String.trimLeft value
        | Multiple value -> command.CommandText <- String.stitch value

        commandDefinition.Parameters
        |> List.iter(fun (name, value) ->
            let parameter = deps.DbValueToParameter name value
            command.Parameters.Add(parameter) |> ignore)

        command.CommandTimeout <- int32 commandDefinition.Timeout.TotalMilliseconds
        command.CommandType <- commandDefinition.CommandType

        if commandDefinition.Prepare then command.Prepare()

        command

    let private setupConnection (connection: #DbConnection) cancellationToken =
        connection.OpenAsync(cancellationToken)
        |> Async.AwaitTask

    let private setupConnectionSync (connection: #DbConnection) =
        connection.Open()

    let private log4 configuration commandDefinition sqlLog =
        match commandDefinition.Logger with
        | Configuration -> configuration.DefaultLogger
        | Override logging -> Some logging
        | Nothing -> None
        |> Option.iter (fun f -> f sqlLog)

    type private ReadState = { Continue: bool; SetIndex: int32; RecordIndex: int32 }

    [<RequireQualifiedAccess>]
    module private ReadState =
        let nextRecord state = { state with RecordIndex = state.RecordIndex + 1 }
        let nextSet state = { state with SetIndex = state.SetIndex + 1; RecordIndex = 0 }
        let stop state = { state with Continue = false }

    let private tryReadNextResultRecord state (dataReader: #DbDataReader) cancellationToken =
        async {
            let! nextResultReadOk = dataReader.AwaitTryReadNextResult(cancellationToken)
            if nextResultReadOk then return ReadState.nextSet state
            else return ReadState.stop state
        }

    let private tryReadNextResultRecordSync state (dataReader: #DbDataReader) =
        let nextResultReadOk = dataReader.TryReadNextResult()
        if nextResultReadOk then ReadState.nextSet state
        else ReadState.stop state

    let private readNextRecord state (dataReader: #DbDataReader) cancellationToken =
        async {
            let! readOk = dataReader.AwaitRead(cancellationToken)
            if readOk then return ReadState.nextRecord state
            else return! tryReadNextResultRecord state dataReader cancellationToken
        }

    let private readNextRecordSync state (dataReader: #DbDataReader) =
        let readOk = dataReader.Read()
        if readOk then ReadState.nextRecord state
        else tryReadNextResultRecordSync state dataReader

    /// Execute the command and return the sets of rows as an AsyncSeq accordingly to the command definition.
    /// Note: This function runs asynchronously.
    let queryAsyncSeq (connection: #DbConnection) deps conf
        (read: Read<_, _>) commandDefinition =
        asyncSeq {
            let! linkedToken = Async.linkedTokenSourceFrom commandDefinition.CancellationToken
            let wasClosed = connection.State = ConnectionState.Closed
            let log sqlLog = log4 conf commandDefinition sqlLog
            let connectionStopwatch = Stopwatch()
            let commandStopwatch = Stopwatch()
            use! command = setupCommand deps commandDefinition linkedToken connection

            try
                if wasClosed then
                    do! setupConnection connection linkedToken
                    connectionStopwatch.Start()
                    ConnectionOpened connection |> log

                CommandPrepared command |> log
                commandStopwatch.Start ()
                use! dbDataReader = deps.ExecuteReaderAsync command linkedToken |> Async.AwaitTask
                let items =
                    AsyncSeq.initInfinite(fun _ -> (dbDataReader, linkedToken))
                    |> SkipFirstAsyncSeq.scanAsync(
                        fun state (dataReader, cancellationToken) ->
                            readNextRecord state dataReader cancellationToken )
                            { Continue = true; SetIndex = 0; RecordIndex = -1 }
                    |> AsyncSeq.takeWhile(fun state -> state.Continue)
                    |> AsyncSeq.mapChange(fun state -> state.SetIndex) (fun _ -> SqlRecordReader(dbDataReader))
                    |> AsyncSeq.mapAsync(fun (state, rowReader) -> async {
                        return read state.SetIndex state.RecordIndex rowReader
                    })
                yield! items

            finally
                commandStopwatch.Stop ()
                CommandExecuted (command, commandStopwatch.Elapsed) |> log
                if wasClosed then
                    connection.Close()
                    connectionStopwatch.Stop ()
                    ConnectionClosed (connection, connectionStopwatch.Elapsed) |> log
        }

    /// Execute the command and return the sets of rows as an seq accordingly to the command definition.
    /// Note: This function runs synchronously.
    let querySeqSync (connection: #DbConnection) deps conf
        (read: Read<_, _>) commandDefinition =
        let wasClosed = connection.State = ConnectionState.Closed
        let log sqlLog = log4 conf commandDefinition sqlLog
        let connectionStopwatch = Stopwatch()
        let commandStopwatch = Stopwatch()
        use command = setupCommandSync deps commandDefinition connection

        try
            if wasClosed then
                setupConnectionSync connection
                connectionStopwatch.Start()
                ConnectionOpened connection |> log

            CommandPrepared command |> log
            commandStopwatch.Start ()
            use dbDataReader = deps.ExecuteReader command
            let items =
                Seq.initInfinite(fun _ -> (dbDataReader))
                |> SkipFirstSeq.scan(readNextRecordSync) { Continue = true; SetIndex = 0; RecordIndex = -1 }
                |> Seq.takeWhile(fun state -> state.Continue)
                |> Seq.mapChange(fun state -> state.SetIndex) (fun _ -> SqlRecordReader(dbDataReader))
                |> Seq.map(fun (state, rowReader) -> read state.SetIndex state.RecordIndex rowReader )
            items

        finally
            commandStopwatch.Stop ()
            CommandExecuted (command, commandStopwatch.Elapsed) |> log
            if wasClosed then
                connection.Close()
                connectionStopwatch.Stop ()
                ConnectionClosed (connection, connectionStopwatch.Elapsed) |> log

    /// Execute the command and return the sets of rows as a list accordingly to the command definition.
    /// Note: This function runs asynchronously.
    let queryList connection deps conf
        (read: Read<_, _>) commandDefinition =
        queryAsyncSeq connection deps conf read commandDefinition
        |> AsyncSeq.toListAsync

    /// Execute the command and return the sets of rows as a list accordingly to the command definition.
    /// Note: This function runs synchronously.
    let queryListSync connection deps conf
        (read: Read<_, _>) commandDefinition =
        querySeqSync connection deps conf read commandDefinition
        |> Seq.toList

    /// Execute the command and return the first set of rows as a list accordingly to the command definition.
    /// Note: This function runs asynchronously.
    let querySetList (connection: #DbConnection) deps conf
        (read: ReadSet<_, _>) commandDefinition =
        async {
            let setList = ResizeArray()
            let readRecord setIndex recordIndex recordReader =
                if setIndex = 0 then setList.Add(read recordIndex recordReader)
                else ()
            do! queryAsyncSeq connection deps conf readRecord commandDefinition |> AsyncSeq.consume
            return setList |> Seq.toList
        }

    /// Execute the command and return the first set of rows as a list accordingly to the command definition.
    /// Note: This function runs synchronously.
    let querySetListSync (connection: #DbConnection) deps conf
        (read: ReadSet<_, _>) commandDefinition =
        let setList = ResizeArray()
        let readRecord setIndex recordIndex recordReader =
            if setIndex = 0 then setList.Add(read recordIndex recordReader)
            else ()
        querySeqSync connection deps conf readRecord commandDefinition |> Seq.consume
        setList |> Seq.toList

    /// Return the 2 first sets of rows as a tuple of 2 lists accordingly to the command definition.
    /// Note: This function runs asynchronously.
    let querySetList2 connection deps conf
        (read1: ReadSet<_, _>) (read2: ReadSet<_, _>) commandDefinition =
        async {
            let set1List = ResizeArray()
            let set2List = ResizeArray()
            let readRecord setIndex recordIndex recordReader =
                if   setIndex = 0 then set1List.Add(read1 recordIndex recordReader)
                elif setIndex = 1 then set2List.Add(read2 recordIndex recordReader)
                else ()
            do! queryAsyncSeq connection deps conf readRecord commandDefinition |> AsyncSeq.consume
            return (set1List |> Seq.toList, set2List |> Seq.toList)
        }

    /// Return the 2 first sets of rows as a tuple of 2 lists accordingly to the command definition.
    /// Note: This function runs synchronously.
    let querySetList2Sync connection deps conf
        (read1: ReadSet<_, _>) (read2: ReadSet<_, _>) commandDefinition =
        let set1List = ResizeArray()
        let set2List = ResizeArray()
        let readRecord setIndex recordIndex recordReader =
            if   setIndex = 0 then set1List.Add(read1 recordIndex recordReader)
            elif setIndex = 1 then set2List.Add(read2 recordIndex recordReader)
            else ()
        querySeqSync connection deps conf readRecord commandDefinition |> Seq.consume
        (set1List |> Seq.toList, set2List |> Seq.toList)

    /// Return the 3 first sets of rows as a tuple of 3 lists accordingly to the command definition.
    /// Note: This function runs asynchronously.
    let querySetList3 connection deps conf
        (read1: ReadSet<_, _>) (read2: ReadSet<_, _>) (read3: ReadSet<_, _>) commandDefinition =
        async {
            let set1List = ResizeArray()
            let set2List = ResizeArray()
            let set3List = ResizeArray()
            let readRecord setIndex recordIndex recordReader =
                if   setIndex = 0 then set1List.Add(read1 recordIndex recordReader)
                elif setIndex = 1 then set2List.Add(read2 recordIndex recordReader)
                elif setIndex = 2 then set3List.Add(read3 recordIndex recordReader)
                else ()
            do! queryAsyncSeq connection deps conf readRecord commandDefinition |> AsyncSeq.consume
            return (set1List |> Seq.toList, set2List |> Seq.toList, set3List |> Seq.toList)
        }

    /// Return the 3 first sets of rows as a tuple of 3 lists accordingly to the command definition.
    /// Note: This function runs synchronously.
    let querySetList3Sync connection deps conf
        (read1: ReadSet<_, _>) (read2: ReadSet<_, _>) (read3: ReadSet<_, _>) commandDefinition =
        let set1List = ResizeArray()
        let set2List = ResizeArray()
        let set3List = ResizeArray()
        let readRecord setIndex recordIndex recordReader =
            if   setIndex = 0 then set1List.Add(read1 recordIndex recordReader)
            elif setIndex = 1 then set2List.Add(read2 recordIndex recordReader)
            elif setIndex = 2 then set3List.Add(read3 recordIndex recordReader)
            else ()
        querySeqSync connection deps conf readRecord commandDefinition |> Seq.consume
        (set1List |> Seq.toList, set2List |> Seq.toList, set3List |> Seq.toList)

    /// Execute the command accordingly to its definition and,
    /// - return the first cell value, if it is available and of the given type.
    /// - throw an exception, otherwise.
    /// Note: This function runs asynchronously.
    let executeScalar<'Scalar, .. > (connection: #DbConnection) deps conf commandDefinition =
        async {
            let! linkedToken = Async.linkedTokenSourceFrom commandDefinition.CancellationToken
            let wasClosed = connection.State = ConnectionState.Closed
            let log sqlLog = log4 conf commandDefinition sqlLog
            let connectionStopwatch = Stopwatch()
            let commandStopwatch = Stopwatch()
            use! command = setupCommand deps commandDefinition linkedToken connection

            try
                if wasClosed then
                    do! setupConnection connection linkedToken
                    connectionStopwatch.Start()
                    ConnectionOpened connection |> log

                CommandPrepared command |> log
                commandStopwatch.Start ()
                use! dataReader = deps.ExecuteReaderAsync command linkedToken |> Async.AwaitTask
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
                commandStopwatch.Stop ()
                CommandExecuted (command, commandStopwatch.Elapsed) |> log
                if wasClosed then
                    connection.Close()
                    connectionStopwatch.Stop ()
                    ConnectionClosed (connection, connectionStopwatch.Elapsed) |> log
        }

    /// Execute the command accordingly to its definition and,
    /// - return the first cell value, if it is available and of the given type.
    /// - throw an exception, otherwise.
    /// Note: This function runs synchronously.
    let executeScalarSync<'Scalar, .. > (connection: #DbConnection) deps conf commandDefinition =
        let wasClosed = connection.State = ConnectionState.Closed
        let log sqlLog = log4 conf commandDefinition sqlLog
        let connectionStopwatch = Stopwatch()
        let commandStopwatch = Stopwatch()
        use command = setupCommandSync deps commandDefinition connection

        try
            if wasClosed then
                setupConnectionSync connection
                connectionStopwatch.Start()
                ConnectionOpened connection |> log

            CommandPrepared command |> log
            commandStopwatch.Start ()
            use dataReader = deps.ExecuteReader command
            let anyData = dataReader.Read()
            if not anyData then
                raise SqlNoDataAvailableException
            else
                // https://github.com/npgsql/npgsql/issues/2087
                if dataReader.IsDBNull(0) && DbNull.is<'Scalar>() then
                    DbNull.retypedAs<'Scalar>()
                else
                    dataReader.GetFieldValue<'Scalar>(0)
        finally
            commandStopwatch.Stop ()
            CommandExecuted (command, commandStopwatch.Elapsed) |> log
            if wasClosed then
                connection.Close()
                connectionStopwatch.Stop ()
                ConnectionClosed (connection, connectionStopwatch.Elapsed) |> log

    /// Execute the command accordingly to its definition and,
    /// - return Some, if the first cell is available and of the given type.
    /// - return None, if first cell is DBNull.
    /// - throw an exception, otherwise.
    /// Note: This function runs asynchronously.
    let executeScalarOrNone<'Scalar, .. > (connection: #DbConnection) deps conf commandDefinition =
        async {
            let! linkedToken = Async.linkedTokenSourceFrom commandDefinition.CancellationToken
            let wasClosed = connection.State = ConnectionState.Closed
            let log sqlLog = log4 conf commandDefinition sqlLog
            let connectionStopwatch = Stopwatch()
            let commandStopwatch = Stopwatch()
            use! command = setupCommand deps commandDefinition linkedToken connection

            try
                if wasClosed then
                    do! setupConnection connection linkedToken
                    connectionStopwatch.Start()
                    ConnectionOpened connection |> log

                CommandPrepared command |> log
                commandStopwatch.Start ()
                use! dataReader = deps.ExecuteReaderAsync command linkedToken |> Async.AwaitTask
                let! anyData = dataReader.ReadAsync(linkedToken) |> Async.AwaitTask
                if not anyData then
                    return raise SqlNoDataAvailableException
                else
                    if dataReader.IsDBNull(0) then return None
                    else return Some (dataReader.GetFieldValue<'Scalar>(0))
            finally
                commandStopwatch.Stop ()
                CommandExecuted (command, commandStopwatch.Elapsed) |> log
                if wasClosed then
                    connection.Close()
                    connectionStopwatch.Stop ()
                    ConnectionClosed (connection, connectionStopwatch.Elapsed) |> log
        }

    /// Execute the command accordingly to its definition and,
    /// - return Some, if the first cell is available and of the given type.
    /// - return None, if first cell is DBNull.
    /// - throw an exception, otherwise.
    /// Note: This function runs asynchronously.
    let executeScalarOrNoneSync<'Scalar, .. > (connection: #DbConnection) deps conf commandDefinition =
        let wasClosed = connection.State = ConnectionState.Closed
        let log sqlLog = log4 conf commandDefinition sqlLog
        let connectionStopwatch = Stopwatch()
        let commandStopwatch = Stopwatch()
        use command = setupCommandSync deps commandDefinition connection

        try
            if wasClosed then
                setupConnectionSync connection
                connectionStopwatch.Start()
                ConnectionOpened connection |> log

            CommandPrepared command |> log
            commandStopwatch.Start ()
            use dataReader = deps.ExecuteReader command
            let anyData = dataReader.Read()
            if not anyData then
                raise SqlNoDataAvailableException
            else
                if dataReader.IsDBNull(0) then None
                else Some (dataReader.GetFieldValue<'Scalar>(0))
        finally
            commandStopwatch.Stop ()
            CommandExecuted (command, commandStopwatch.Elapsed) |> log
            if wasClosed then
                connection.Close()
                connectionStopwatch.Stop ()
                ConnectionClosed (connection, connectionStopwatch.Elapsed) |> log

    /// Execute the command accordingly to its definition and, return the number of rows affected.
    /// Note: This function runs asynchronously.
    let executeNonQuery (connection: #DbConnection) deps conf commandDefinition =
        async {
            let! linkedToken = Async.linkedTokenSourceFrom commandDefinition.CancellationToken
            let wasClosed = connection.State = ConnectionState.Closed
            let log sqlLog = log4 conf commandDefinition sqlLog
            let connectionStopwatch = Stopwatch()
            let commandStopwatch = Stopwatch()
            use! command = setupCommand deps commandDefinition linkedToken connection

            try
                if wasClosed then
                    do! setupConnection connection linkedToken
                    connectionStopwatch.Start()
                    ConnectionOpened connection |> log

                CommandPrepared command |> log
                commandStopwatch.Start ()
                return! command.ExecuteNonQueryAsync(linkedToken) |> Async.AwaitTask
            finally
                commandStopwatch.Stop ()
                CommandExecuted (command, commandStopwatch.Elapsed) |> log
                if wasClosed then
                    connection.Close()
                    connectionStopwatch.Stop ()
                    ConnectionClosed (connection, connectionStopwatch.Elapsed) |> log
        }

    /// Execute the command accordingly to its definition and, return the number of rows affected.
    /// Note: This function runs synchronously.
    let executeNonQuerySync (connection: #DbConnection) deps conf commandDefinition =
        let wasClosed = connection.State = ConnectionState.Closed
        let log sqlLog = log4 conf commandDefinition sqlLog
        let connectionStopwatch = Stopwatch()
        let commandStopwatch = Stopwatch()
        use command = setupCommandSync deps commandDefinition connection

        try
            if wasClosed then
                setupConnectionSync connection
                connectionStopwatch.Start()
                ConnectionOpened connection |> log

            CommandPrepared command |> log
            commandStopwatch.Start ()
            command.ExecuteNonQuery()
        finally
            commandStopwatch.Stop ()
            CommandExecuted (command, commandStopwatch.Elapsed) |> log
            if wasClosed then
                connection.Close()
                connectionStopwatch.Stop ()
                ConnectionClosed (connection, connectionStopwatch.Elapsed) |> log
