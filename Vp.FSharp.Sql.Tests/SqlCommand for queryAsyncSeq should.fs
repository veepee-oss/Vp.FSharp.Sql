module Vp.FSharp.Sql.Tests.``SqlCommand for queryAsyncSeq should``

open System
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
                   (fun _ _ reader ->
                        [2;1;0]
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
                   (fun _ _ reader -> [2;1;0] |> List.map (reader.Value >> int))
                |> AsyncSeq.toListSynchronously
                |> List.sortBy (fun values -> values.[0])
        r.Length =! 2
        r =! [[3;2;1];[6;5;4]]
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``queryAsyncSeq should open and close the connection when it's closed and access valueOrNone by columnName`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
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
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.queryAsyncSeq connection (Mocks.makeDependencies None None)
                   (fun _ _ reader ->
                       [2;1;0]
                       |> List.map (sprintf "id%i")
                       |> List.map (fun fieldName -> (fieldName, reader.ValueOrNone fieldName |> Option.map int))
                   )
                |> AsyncSeq.toListSynchronously
                |> List.sortBy (fun values -> snd values.[0])
        r.Length =! 2
        r =! [[("id2", Some 3);("id1", None);("id0", Some 1)];[("id2", Some 6);("id1", Some 5);("id0", Some 4)]]
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``queryAsyncSeq should open and close the connection when it's closed and access valueOrNone by ordinal`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
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
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let r = SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.queryAsyncSeq connection (Mocks.makeDependencies None None)
                   (fun _ _ reader -> [2;1;0] |> List.map (reader.ValueOrNone >> Option.map int))
                |> AsyncSeq.toListSynchronously
                |> List.sortBy (fun values -> values.[0])
        r.Length =! 2
        r =! [[Some 3; None; Some 1];[Some 6; Some 5; Some 4]]
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``queryAsyncSeq should let the connection when it's other than closed with access valueOrNone by ordinal`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
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
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let r = SqlCommand.text "select 1"
                 |> SqlCommand.noLogger
                 |> SqlCommand.queryAsyncSeq connection (Mocks.makeDependencies None None)
                    (fun _ _ reader -> [2;1;0] |> List.map (reader.ValueOrNone >> Option.map int))
                 |> AsyncSeq.toListSynchronously
                 |> List.sortBy (fun values -> values.[0])
        r.Length =! 2
        r =! [[Some 3; None; Some 1];[Some 6; Some 5; Some 4]]
        !openCall =! 0
        !closeCall =! 0
    }

[<Fact>]
let ``queryAsyncSeq should log for all events on globalLogger when the connection is closed with access valueOrNone by ordinal`` () =
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
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let r = SqlCommand.text "select 1"
                 |> SqlCommand.queryAsyncSeq connection deps
                    (fun _ _ reader -> [2;1;0] |> List.map (reader.ValueOrNone >> Option.map int))
                 |> AsyncSeq.toListSynchronously
                 |> List.sortBy (fun values -> values.[0])
        r.Length =! 2
        r =! [[Some 3; None; Some 1];[Some 6; Some 5; Some 4]]
        !openCall =! 1
        !closeCall =! 1
        !connectionOpened =! 1
        !connectionClosed =! 1
        !commandPrepared =! 1
        !commandExecuted =! 1
    }

[<Fact>]
let ``queryAsyncSeq should log for just command events on globalLogger when the connection is NOT closed with access valueOrNone by ordinal`` () =
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
            Mocks.makeReader data
            |> Mocks.makeConnection "toto" ConnectionState.Connecting openCallback closeCallback
        let deps = Some loggerCallback
                   |> Mocks.makeDependencies None
        let r = SqlCommand.text "select 1"
                |> SqlCommand.queryAsyncSeq connection deps
                   (fun _ _ reader -> [2;1;0] |> List.map (reader.ValueOrNone >> Option.map int))
                |> AsyncSeq.toListSynchronously
                |> List.sortBy (fun values -> values.[0])
        r.Length =! 2
        r =! [[Some 3; None; Some 1];[Some 6; Some 5; Some 4]]
        !openCall =! 0
        !closeCall =! 0
        !connectionOpened =! 0
        !connectionClosed =! 0
        !commandPrepared =! 1
        !commandExecuted =! 1
    }
