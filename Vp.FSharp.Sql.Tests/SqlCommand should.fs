module Vp.FSharp.Sql.Tests.``SqlCommand should``

open System.Data
open Swensen.Unquote
open Xunit

open Vp.FSharp.Sql


[<Fact>]
let ``executeScalar should open and close the connection when it's closed`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    async {
        use connection =
            Mocks.makeReader None
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalar connection (Mocks.makeDependencies None None)
        r =! null
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``executeScalar should let the connection when it's other than closed`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    async {
        use connection =
            Mocks.makeReader None
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalar connection (Mocks.makeDependencies None None)
        r =! null
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
    async {
        use connection =
            Mocks.makeReader None
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalar connection deps
        r =! null
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
    async {
        use connection =
            Mocks.makeReader None
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalar connection deps
        r =! null
        !openCall =! 0
        !closeCall =! 0
        !connectionOpened =! 0
        !connectionClosed =! 0
        !commandPrepared =! 1
        !commandExecuted =! 1
    }
