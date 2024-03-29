module Vp.FSharp.Sql.Tests.``SqlCommand for executeScalar should``

open System.Data

open Swensen.Unquote

open Xunit

open Vp.FSharp.Sql
open Vp.FSharp.Sql.Tests.Helpers


[<Fact>]
let ``open and then close the connection if initially closed`` () =
    let callCounter = PartialCallCounter.initSame 0
    let openCallback, closeCallback = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [14]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name }
                ]]

    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let! outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.noLogger
            |> SqlCommand.executeScalar connection deps conf
        outcome =! 14
        PartialCallCounter.assertEqual callCounter 1 1
    }

[<Fact>]
let ``leave the connection open if initially open`` () =
    let callCounter = PartialCallCounter.initSame 0
    let openCallback, closeCallback = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [15]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name }
                ]]

    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let! outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.noLogger
            |> SqlCommand.executeScalar connection deps conf
        outcome =! 15
        PartialCallCounter.assertEqual callCounter 0 0
    }

[<Fact>]
let ``log for all events on globalLogger if connection initially closed`` () =
    let callCounter = FullCallCounter.initSame 0
    let openCallback, closeCallback, loggerCallback = FullCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [16]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf (Some loggerCallback)
        let! outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.executeScalar connection deps conf
        outcome =! 16
        FullCallCounter.assertEqual callCounter 1 1 1 1 1 1
    }

[<Fact>]
let ``log for just command events on globalLogger if connection initially not closed`` () =
    let callCounter = FullCallCounter.initSame 0
    let openCallback, closeCallback, loggerCallback = FullCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [17]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf (Some loggerCallback)
        let! outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.executeScalar connection deps conf
        outcome =! 17
        FullCallCounter.assertEqual callCounter 0 0 0 0 1 1
    }
