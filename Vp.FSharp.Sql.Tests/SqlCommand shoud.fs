module Vp.FSharp.Sql.Tests.``SqlCommand should``

open System.Data
open Swensen.Unquote
open Xunit

open Vp.FSharp.Sql


[<Fact>]
let ``executeScalar should open and close the connection when it's closed`` () =
    let mutable openCall = 0
    let mutable closeCall = 0
    let openCallback () =
        openCall <- openCall + 1
    let closeCallback () =
        closeCall <- closeCall + 1
    async {
        use connection =
            Mocks.makeReader None
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalar connection (Mocks.makeDependencies None None)
        r =! null
        openCall =! 1
        closeCall =! 1
    }

[<Fact>]
let ``executeScalar should let the connection when it's other than closed`` () =
    let mutable openCall = 0
    let mutable closeCall = 0
    let openCallback () =
        openCall <- openCall + 1
    let closeCallback () =
        closeCall <- closeCall + 1
    async {
        use connection =
            Mocks.makeReader None
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalar connection (Mocks.makeDependencies None None)
        r =! null
        openCall =! 0
        closeCall =! 0
    }

[<Fact>]
let ``executeScalar should log for all events on globalLogger when the connection is closed`` () =
    let mutable openCall = 0
    let mutable closeCall = 0
    let openCallback () =
        openCall <- openCall + 1
    let closeCallback () =
        closeCall <- closeCall + 1
    let mutable connectionOpened = 0
    let mutable connectionClosed = 0
    let mutable commandPrepared = 0
    let mutable commandExecuted = 0
    let loggerCallback =
        function
        | ConnectionOpened connection -> connectionOpened <- connectionOpened + 1
        | ConnectionClosed (connection, sinceOpened) -> connectionClosed <- connectionClosed + 1
        | CommandPrepared command -> commandPrepared <- commandPrepared + 1
        | CommandExecuted (connection, sincePrepared) -> commandExecuted <- commandExecuted + 1
    async {
        use connection =
            Mocks.makeReader None
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalar connection deps
        r =! null
        openCall =! 1
        closeCall =! 1
        connectionOpened =! 1
        connectionClosed =! 1
        commandPrepared =! 1
        commandExecuted =! 1
    }

[<Fact>]
let ``executeScalar should log for just command events on globalLogger when the connection is NOT closed`` () =
    let mutable openCall = 0
    let mutable closeCall = 0
    let openCallback () =
        openCall <- openCall + 1
    let closeCallback () =
        closeCall <- closeCall + 1
    let mutable connectionOpened = 0
    let mutable connectionClosed = 0
    let mutable commandPrepared = 0
    let mutable commandExecuted = 0
    let loggerCallback =
        function
        | ConnectionOpened connection -> connectionOpened <- connectionOpened + 1
        | ConnectionClosed (connection, sinceOpened) -> connectionClosed <- connectionClosed + 1
        | CommandPrepared command -> commandPrepared <- commandPrepared + 1
        | CommandExecuted (connection, sincePrepared) -> commandExecuted <- commandExecuted + 1
    async {
        use connection =
            Mocks.makeReader None
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalar connection deps
        r =! null
        openCall =! 0
        closeCall =! 0
        connectionOpened =! 0
        connectionClosed =! 0
        commandPrepared =! 1
        commandExecuted =! 1
    }
