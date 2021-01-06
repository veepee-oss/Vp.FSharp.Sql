module Vp.FSharp.Sql.Tests.``SqlCommand for executeScalarOrNone should``

open System.Data

open Swensen.Unquote

open Xunit

open Vp.FSharp.Sql
open Vp.FSharp.Sql.Tests.Helpers


[<Fact>]
let ``open and then close the connection if initially closed`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [14]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                ]]

    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalarOrNone connection (Mocks.makeDependencies None None)
        r.IsSome =! true
        r
        |> Option.defaultValue 42
        |> (=!) 14
        PartialCallCounter.assertEqual callCounter 1 1
    }

[<Fact>]
let ``leave the connection open if initially not closed`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [15]
                ]]
                [[
                    { Name = "id"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                ]]

    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalarOrNone connection (Mocks.makeDependencies None None)
        r.IsSome =! true
        r
        |> Option.defaultValue 42
        |> (=!) 15

        PartialCallCounter.assertEqual callCounter 0 0
    }

[<Fact>]
let ``log for all events on globalLogger if connection initially closed`` () =
    let callCounter = FullCallCounter.initSame 0
    let openCallback = FullCallCounter.createOpenCallback callCounter
    let closeCallback = FullCallCounter.createCloseCallback callCounter
    let loggerCallback = FullCallCounter.createLoggerCallback callCounter
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
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalarOrNone connection deps
        r.IsSome =! true
        r
        |> Option.defaultValue 42
        |> (=!) 16
        FullCallCounter.assertEqual callCounter 1 1 1 1 1 1
    }

[<Fact>]
let ``log for just command events on globalLogger if connection initially not closed`` () =
    let callCounter = FullCallCounter.initSame 0
    let openCallback = FullCallCounter.createOpenCallback callCounter
    let closeCallback = FullCallCounter.createCloseCallback callCounter
    let loggerCallback = FullCallCounter.createLoggerCallback callCounter
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
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalarOrNone connection deps
        r.IsSome =! true
        r
        |> Option.defaultValue 42
        |> (=!) 17
        FullCallCounter.assertEqual callCounter 0 0 0 0 1 1
    }


[<Fact>]
let ``open and then close connection if initially closed and retrieve None`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [null]
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
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalarOrNone connection (Mocks.makeDependencies None None)
        r.IsNone =! true
        PartialCallCounter.assertEqual callCounter 1 1
    }

[<Fact>]
let ``leave connection open if initially not closed and retrieve None`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [null]
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
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.executeScalarOrNone connection (Mocks.makeDependencies None None)
        r.IsNone =! true
        PartialCallCounter.assertEqual callCounter 0 0
    }

[<Fact>]
let ``log for all events on globalLogger if connection initially closed and retrieve None`` () =
    let callCounter = FullCallCounter.initSame 0
    let openCallback = FullCallCounter.createOpenCallback callCounter
    let closeCallback = FullCallCounter.createCloseCallback callCounter
    let loggerCallback = FullCallCounter.createLoggerCallback callCounter
    let data = Mocks.fakeData
                [[
                        [null]
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
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalarOrNone connection deps
        r.IsNone =! true
        FullCallCounter.assertEqual callCounter 1 1 1 1 1 1
    }

[<Fact>]
let ``log for just command events on globalLogger if connection initially not closed and retrieve None`` () =
    let callCounter = FullCallCounter.initSame 0
    let openCallback = FullCallCounter.createOpenCallback callCounter
    let closeCallback = FullCallCounter.createCloseCallback callCounter
    let loggerCallback = FullCallCounter.createLoggerCallback callCounter
    let data = Mocks.fakeData
                [[
                        [null]
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
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let! r = SqlCommand.text "select 1"
                |> SqlCommand.executeScalarOrNone connection deps
        r.IsNone =! true
        FullCallCounter.assertEqual callCounter 0 0 0 0 1 1
    }
