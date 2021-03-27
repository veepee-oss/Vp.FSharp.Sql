module Vp.FSharp.Sql.Tests.``SqlCommand for querySetListNSync should``

open System.Data
open System.Data.Common

open Swensen.Unquote

open Xunit

open Vp.FSharp.Sql
open Vp.FSharp.Sql.Tests.Helpers


let private toFieldName =
    sprintf "id%i"

let private boxes values =
    List.map box values

let private makeDbField index : Mocks.DbField' =
    { Name = toFieldName index
      FieldType = typeof<int32>
      NativeTypeName = typeof<int32>.Name }

let private readValueByIndex (columns: int32 list list) indexColumn _ (reader: SqlRecordReader<DbDataReader>) =
    columns.[indexColumn]
    |> List.map (reader.Value >> int32)

[<Fact>]
let ``have querySetListSync should open and then close the connection if initially closed and access value by columnName`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[1..6] |> boxes |> List.splitInto 2
                 [1..8] |> boxes |> List.splitInto 2]
                [[0..(0+2)] |> List.map makeDbField
                 [4..(4+3)] |> List.map makeDbField]
    let columnsIndex = data.Columns |> List.map (List.mapi (fun i _ -> i))
    use connection =
        Mocks.Reader (Mocks.makeReader data)
        |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
    let deps = Mocks.makeDeps None
    let conf = Mocks.makeConf None
    let r0 =
        SqlCommand.text "select 1"
        |> SqlCommand.noLogger
        |> SqlCommand.querySetListSync connection
                deps conf
                (readValueByIndex columnsIndex 0)
    r0.Length =! 2
    r0 =! ([1..6] |> List.splitInto 2)
    PartialCallCounter.assertEqual callCounter 1 1

[<Fact>]
let ``have querySetList2Sync should open and then close the connection if initially closed and access value by columnName`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[1..6] |> boxes |> List.splitInto 2
                 [1..8] |> boxes |> List.splitInto 2]
                [[0..(0+2)] |> List.map makeDbField
                 [4..(4+3)] |> List.map makeDbField]
    let columnsIndex = data.Columns |> List.map (List.mapi (fun i _ -> i))
    use connection =
        Mocks.Reader (Mocks.makeReader data)
        |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
    let deps = Mocks.makeDeps None
    let conf = Mocks.makeConf None
    let (r0, r1) =
        SqlCommand.text "select 1"
        |> SqlCommand.noLogger
        |> SqlCommand.querySetList2Sync connection deps conf
                (readValueByIndex columnsIndex 0)
                (readValueByIndex columnsIndex 1)
    r0.Length =! 2
    r0 =! ([1..6] |> List.splitInto 2)
    r1.Length =! 2
    r1 =! ([1..8] |> List.splitInto 2)
    PartialCallCounter.assertEqual callCounter 1 1

[<Fact>]
let ``have querySetList3Sync open and then close the connection if initially closed and access value by columnName`` () =
    let callCounter = PartialCallCounter.initSame 0
    let (openCallback, closeCallback) = PartialCallCounter.createCallbacks callCounter
    let data = Mocks.fakeData
                [[1..6] |> boxes |> List.splitInto 2
                 [1..8] |> boxes |> List.splitInto 2
                 [1..10] |> boxes |> List.splitInto 2]
                [[0..(0+2)] |> List.map makeDbField
                 [4..(4+3)] |> List.map makeDbField
                 [8..(8+4)] |> List.map makeDbField]
    let columnsIndex = data.Columns |> List.map (List.mapi (fun i _ -> i))
    use connection =
        Mocks.Reader (Mocks.makeReader data)
        |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
    let deps = Mocks.makeDeps None
    let conf = Mocks.makeConf None
    let (r0, r1, r2) =
        SqlCommand.text "select 1"
        |> SqlCommand.noLogger
        |> SqlCommand.querySetList3Sync connection deps conf
                (readValueByIndex columnsIndex 0)
                (readValueByIndex columnsIndex 1)
                (readValueByIndex columnsIndex 2)
    r0.Length =! 2
    r0 =! ([1..6] |> List.splitInto 2)
    r1.Length =! 2
    r1 =! ([1..8] |> List.splitInto 2)
    r2.Length =! 2
    r2 =! ([1..10] |> List.splitInto 2)
    PartialCallCounter.assertEqual callCounter 1 1
