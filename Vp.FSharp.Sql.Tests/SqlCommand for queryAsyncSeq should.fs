module Vp.FSharp.Sql.Tests.``SqlCommand for queryAsyncSeq should``

open System
open System.Data
open System.Data.Common

open FSharp.Control

open Swensen.Unquote

open Xunit

open Vp.FSharp.Sql
open Vp.FSharp.Sql.Tests.Helpers


let toFieldName =
    sprintf "id%i"

let toFieldNames =
    List.map toFieldName

let readValueByFieldName (columns: string list) _ _ (reader: SqlRecordReader<DbDataReader>) =
    columns
    |> List.map (fun fieldName -> (fieldName, reader.Value fieldName |> int32))

let readValueByIndex (columns: int32 list) _ _ (reader: SqlRecordReader<DbDataReader>) =
    columns
    |> List.map (reader.Value >> int32)

let readValueOrNoneByFieldName (columns: string list) _ _ (reader: SqlRecordReader<DbDataReader>) =
    columns
    |> List.map (fun fieldName -> (fieldName, reader.ValueOrNone fieldName |> Option.map int32))

let readValueOrNoneByIndex (columns: int32 list) _ _ (reader: SqlRecordReader<DbDataReader>) =
    columns
    |> List.map (reader.ValueOrNone >> Option.map int32)

[<Fact>]
let ``open and then close the connection if initially closed and access value by columnName`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [1;2;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.noLogger
            |> SqlCommand.queryAsyncSeq connection deps conf
               (readValueByFieldName ([2;1;0] |> toFieldNames))
            |> AsyncSeq.toListSynchronously
            |> List.sortBy (fun values -> snd values.[0])
        outcome.Length =! 2
        outcome =! [[("id2", 3);("id1", 2);("id0", 1)];[("id2", 6);("id1", 5);("id0", 4)]]
        PartialCallCounter.assertEqual callCounter 1 1
    }

[<Fact>]
let ``open and then close the connection if initially closed and access value by ordinal`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [1;2;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.noLogger
            |> SqlCommand.queryAsyncSeq connection deps conf
               (readValueByIndex [2;1;0])
            |> AsyncSeq.toListSynchronously
            |> List.sortBy (fun values -> values.[0])
        outcome.Length =! 2
        outcome =! [[3;2;1];[6;5;4]]
        PartialCallCounter.assertEqual callCounter 1 1
    }

[<Fact>]
let ``open and then close the connection if initially closed and access valueOrNone by columnName`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [1;null;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int Nullable>
                      NativeTypeName = typeof<int>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int>
                      NativeTypeName = typeof<int>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.noLogger
            |> SqlCommand.queryAsyncSeq connection deps conf
               (readValueOrNoneByFieldName (toFieldNames [2;1;0]))
            |> AsyncSeq.toListSynchronously
            |> List.sortBy (fun values -> snd values.[0])
        outcome.Length =! 2
        outcome =! [[("id2", Some 3);("id1", None);("id0", Some 1)];[("id2", Some 6);("id1", Some 5);("id0", Some 4)]]
        PartialCallCounter.assertEqual callCounter 1 1
    }

[<Fact>]
let ``open and then close connection if initially closed and access valueOrNone by ordinal`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [1;null;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int32 Nullable>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.noLogger
            |> SqlCommand.queryAsyncSeq connection deps conf
               (readValueOrNoneByIndex [2;1;0])
            |> AsyncSeq.toListSynchronously
            |> List.sortBy (fun values -> values.[0])
        outcome.Length =! 2
        outcome =! [[Some 3; None; Some 1];[Some 6; Some 5; Some 4]]
        PartialCallCounter.assertEqual callCounter 1 1
    }

[<Fact>]
let ``leave the connection open if initially not closed with access valueOrNone by ordinal`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [1;null;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int32 Nullable>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf None
        let outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.noLogger
            |> SqlCommand.queryAsyncSeq connection deps conf
               (readValueOrNoneByIndex [2;1;0])
            |> AsyncSeq.toListSynchronously
            |> List.sortBy (fun values -> values.[0])
        outcome.Length =! 2
        outcome =! [[Some 3; None; Some 1];[Some 6; Some 5; Some 4]]
        PartialCallCounter.assertEqual callCounter 0 0
    }

[<Fact>]
let ``log all events on globalLogger if connection initially closed with access valueOrNone by ordinal`` () =
    let callCounter = FullCallCounter.initSame 0
    let (openCallback, closeCallback, loggerCallback) = FullCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [1;null;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int32 Nullable>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf (Some loggerCallback)
        let outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.queryAsyncSeq connection deps conf
               (readValueOrNoneByIndex [2;1;0])
            |> AsyncSeq.toListSynchronously
            |> List.sortBy (fun values -> values.[0])
        outcome.Length =! 2
        outcome =! [[Some 3; None; Some 1];[Some 6; Some 5; Some 4]]
        FullCallCounter.assertEqual callCounter 1 1 1 1 1 1
    }

[<Fact>]
let ``log for just command events on globalLogger if connection initially not closed with access valueOrNone by ordinal`` () =
    let callCounter = FullCallCounter.initSame 0
    let (openCallback, closeCallback, loggerCallback) = FullCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[
                        [1;null;3]
                        [4;5;6]
                ]]
                [[
                    { Name = "id0"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id1"
                      FieldType = typeof<int32 Nullable>
                      NativeTypeName = typeof<int32>.Name
                    }
                    { Name = "id2"
                      FieldType = typeof<int32>
                      NativeTypeName = typeof<int32>.Name
                    }
                ]]
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Mocks.makeDeps None
        let conf = Mocks.makeConf (Some loggerCallback)
        let outcome =
            SqlCommand.text "select 1"
            |> SqlCommand.queryAsyncSeq connection deps conf
               (readValueOrNoneByIndex [2;1;0])
            |> AsyncSeq.toListSynchronously
            |> List.sortBy (fun values -> values.[0])
        outcome.Length =! 2
        outcome =! [[Some 3; None; Some 1];[Some 6; Some 5; Some 4]]
        FullCallCounter.assertEqual callCounter 0 0 0 0 1 1
    }
