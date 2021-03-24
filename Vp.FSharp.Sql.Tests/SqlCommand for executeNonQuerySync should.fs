module Vp.FSharp.Sql.Tests.``SqlCommand for executeNonQuerySync should``

open System.Data

open Swensen.Unquote

open Xunit

open Vp.FSharp.Sql
open Vp.FSharp.Sql.Tests.Helpers


[<Fact>]
let ``open and then close the connection if initially closed`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    async {
        use connection =
            Mocks.NonQuery 0
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let! outcome =
            SqlCommand.text "update"
            |> SqlCommand.noLogger
            |> SqlCommand.executeNonQuery connection deps conf
        outcome =! 0
        PartialCallCounter.assertEqual callCounter 1 1
    }

[<Fact>]
let ``leave the connection open if initially open`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    async {
        use connection =
            Mocks.NonQuery 1
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let! outcome =
            SqlCommand.text "update"
            |> SqlCommand.noLogger
            |> SqlCommand.executeNonQuery connection deps conf
        outcome =! 1
        PartialCallCounter.assertEqual callCounter 0 0
    }

[<Fact>]
let ``log for all events on globalLogger if connection initially closed`` () =
    let callCounter = FullCallCounter.initSame 0
    let (openCallback, closeCallback, loggerCallback) = FullCallCounter.createCallbacks callCounter
    async {
        use connection =
            Mocks.NonQuery 2
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf (Some loggerCallback)
        let! outcome =
            SqlCommand.text "update"
            |> SqlCommand.executeNonQuery connection deps conf
        outcome =! 2
        FullCallCounter.assertEqual callCounter 1 1 1 1 1 1
    }

[<Fact>]
let ``log just command events on globalLogger if connection initially not closed`` () =
    let callCounter = FullCallCounter.initSame 0
    let (openCallback, closeCallback, loggerCallback) = FullCallCounter.createCallbacks callCounter
    async {
        use connection =
            Mocks.NonQuery 3
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf (Some loggerCallback)
        let! outcome =
            SqlCommand.text "update"
            |> SqlCommand.executeNonQuery connection deps conf
        outcome =! 3
        FullCallCounter.assertEqual callCounter 0 0 0 0 1 1
    }
