module Vp.FSharp.Sql.Tests.``SqlCommand for queryAsyncSeq should``

open System.Data
open FSharp.Control
open Swensen.Unquote
open Xunit

open Vp.FSharp.Sql


[<Fact>]
let ``queryAsyncSeq should open and close the connection when it's closed and access value by columnName`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[
                        [1;2;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.queryAsyncSeq connection (Mocks.makeDependencies None None)
                   (fun _ _ reader -> [2;1;0]
                                      |> List.map (sprintf "id%i")
                                      |> List.map (fun fieldName -> (fieldName, reader.Value fieldName |> int))
                   )
                |> AsyncSeq.toListSynchronously
                |> List.sortBy (fun values -> snd values.[0])
        r.Length =! 2
        r =! [[("id2", 3);("id1", 2);("id0", 1)];[("id2", 6);("id1", 5);("id0", 4)]]
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``queryAsyncSeq should open and close the connection when it's closed and access value by ordinal`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[
                        [1;2;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.queryAsyncSeq connection (Mocks.makeDependencies None None)
                   (fun _ _ reader -> [2;1;0]
                                      |> List.map (reader.Value >> int)
                   )
                |> AsyncSeq.toListSynchronously
                |> List.sortBy (fun values -> values.[0])
        r.Length =! 2
        r =! [[3;2;1];[6;5;4]]
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``executeScalar should let the connection when it's other than closed`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[
                        [15]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                ]]

    async {
        use connection =
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalar connection (Mocks.makeDependencies None None)
        r =! 15
        !openCall =! 0
        !closeCall =! 0
    }

[<Fact>]
let ``executeScalar should log for all events on globalLogger when the connection is closed`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let connectionOpened = ref 0
    let connectionClosed = ref 0
    let commandPrepared = ref 0
    let commandExecuted = ref 0
    let loggerCallback =
        function
        | ConnectionOpened connection -> incr connectionOpened
        | ConnectionClosed (connection, sinceOpened) -> incr connectionClosed
        | CommandPrepared command -> incr commandPrepared
        | CommandExecuted (connection, sincePrepared) -> incr commandExecuted
    let data = Mocks.fakeData
                [[
                        [16]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalar connection deps
        r =! 16
        !openCall =! 1
        !closeCall =! 1
        !connectionOpened =! 1
        !connectionClosed =! 1
        !commandPrepared =! 1
        !commandExecuted =! 1
    }

[<Fact>]
let ``executeScalar should log for just command events on globalLogger when the connection is NOT closed`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let connectionOpened = ref 0
    let connectionClosed = ref 0
    let commandPrepared = ref 0
    let commandExecuted = ref 0
    let loggerCallback =
        function
        | ConnectionOpened connection -> incr connectionOpened
        | ConnectionClosed (connection, sinceOpened) -> incr connectionClosed
        | CommandPrepared command -> incr commandPrepared
        | CommandExecuted (connection, sincePrepared) -> incr commandExecuted
    let data = Mocks.fakeData
                [[
                        [17]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalar connection deps
        r =! 17
        !openCall =! 0
        !closeCall =! 0
        !connectionOpened =! 0
        !connectionClosed =! 0
        !commandPrepared =! 1
        !commandExecuted =! 1
    }
