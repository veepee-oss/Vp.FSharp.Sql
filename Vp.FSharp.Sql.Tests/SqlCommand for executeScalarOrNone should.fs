module Vp.FSharp.Sql.Tests.``SqlCommand for executeScalarOrNone should``

open System.Data
open Swensen.Unquote
open Xunit

open Vp.FSharp.Sql


[<Fact>]
let ``executeScalarOrNone should open and close the connection when it's closed`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[
                        [14]
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
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalarOrNone connection (Mocks.makeDependencies None None)
        r.IsSome =! true
        r
        |> Option.defaultValue 42
        |> (=!) 14
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``executeScalarOrNone should let the connection when it's other than closed`` () =
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
                |> SqlCommand.executeScalarOrNone connection (Mocks.makeDependencies None None)
        r.IsSome =! true
        r
        |> Option.defaultValue 42
        |> (=!) 15

        !openCall =! 0
        !closeCall =! 0
    }

[<Fact>]
let ``executeScalarOrNone should log for all events on globalLogger when the connection is closed`` () =
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
                |> SqlCommand.executeScalarOrNone connection deps
        r.IsSome =! true
        r
        |> Option.defaultValue 42
        |> (=!) 16
        !openCall =! 1
        !closeCall =! 1
        !connectionOpened =! 1
        !connectionClosed =! 1
        !commandPrepared =! 1
        !commandExecuted =! 1
    }

[<Fact>]
let ``executeScalarOrNone should log for just command events on globalLogger when the connection is NOT closed`` () =
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
                |> SqlCommand.executeScalarOrNone connection deps
        r.IsSome =! true
        r
        |> Option.defaultValue 42
        |> (=!) 17
        !openCall =! 0
        !closeCall =! 0
        !connectionOpened =! 0
        !connectionClosed =! 0
        !commandPrepared =! 1
        !commandExecuted =! 1
    }


[<Fact>]
let ``executeScalarOrNone should open and close the connection when it's closed and retrieve None`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[
                        [null]
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
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalarOrNone connection (Mocks.makeDependencies None None)
        r.IsNone =! true
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``executeScalarOrNone should let the connection when it's other than closed and retrieve None`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[
                        [null]
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
                |> SqlCommand.executeScalarOrNone connection (Mocks.makeDependencies None None)
        r.IsNone =! true
        !openCall =! 0
        !closeCall =! 0
    }

[<Fact>]
let ``executeScalarOrNone should log for all events on globalLogger when the connection is closed and retrieve None`` () =
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
                        [null]
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
                |> SqlCommand.executeScalarOrNone connection deps
        r.IsNone =! true
        !openCall =! 1
        !closeCall =! 1
        !connectionOpened =! 1
        !connectionClosed =! 1
        !commandPrepared =! 1
        !commandExecuted =! 1
    }

[<Fact>]
let ``executeScalarOrNone should log for just command events on globalLogger when the connection is NOT closed and retrieve None`` () =
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
                        [null]
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
                |> SqlCommand.executeScalarOrNone connection deps
        r.IsNone =! true
        !openCall =! 0
        !closeCall =! 0
        !connectionOpened =! 0
        !connectionClosed =! 0
        !commandPrepared =! 1
        !commandExecuted =! 1
    }
