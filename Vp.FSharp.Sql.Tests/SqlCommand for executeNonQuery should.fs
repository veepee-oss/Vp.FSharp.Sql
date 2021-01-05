module Vp.FSharp.Sql.Tests.``SqlCommand for executeNonQuery should``

open System.Data
open Swensen.Unquote
open Xunit

open Vp.FSharp.Sql


[<Fact>]
let ``executeNonQuery should open and close the connection when it's closed`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall

    async {
        use connection =
            Mocks.NonQuery 0
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let! r = SqlCommand.text "update"
                |> SqlCommand.noLogger
                |> SqlCommand.executeNonQuery connection (Mocks.makeDependencies None None)
        r =! 0
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``executeNonQuery should let the connection when it's other than closed`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall

    async {
        use connection =
            Mocks.NonQuery 1
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let! r = SqlCommand.text "update"
                |> SqlCommand.noLogger
                |> SqlCommand.executeNonQuery connection (Mocks.makeDependencies None None)
        r =! 1
        !openCall =! 0
        !closeCall =! 0
    }

[<Fact>]
let ``executeNonQuery should log for all events on globalLogger when the connection is closed`` () =
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
        | ConnectionOpened _ -> incr connectionOpened
        | ConnectionClosed _ -> incr connectionClosed
        | CommandPrepared _ -> incr commandPrepared
        | CommandExecuted _ -> incr commandExecuted
    async {
        use connection =
            Mocks.NonQuery 2
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "update"
                |> SqlCommand.executeNonQuery connection deps
        r =! 2
        !openCall =! 1
        !closeCall =! 1
        !connectionOpened =! 1
        !connectionClosed =! 1
        !commandPrepared =! 1
        !commandExecuted =! 1
    }

[<Fact>]
let ``executeNonQuery should log for just command events on globalLogger when the connection is NOT closed`` () =
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
        | ConnectionOpened _ -> incr connectionOpened
        | ConnectionClosed _ -> incr connectionClosed
        | CommandPrepared _ -> incr commandPrepared
        | CommandExecuted _ -> incr commandExecuted
    async {
        use connection =
            Mocks.NonQuery 3
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "update"
                |> SqlCommand.executeNonQuery connection deps
        r =! 3
        !openCall =! 0
        !closeCall =! 0
        !connectionOpened =! 0
        !connectionClosed =! 0
        !commandPrepared =! 1
        !commandExecuted =! 1
    }
