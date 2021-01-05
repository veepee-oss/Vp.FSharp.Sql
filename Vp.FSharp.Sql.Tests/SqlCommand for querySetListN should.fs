module Vp.FSharp.Sql.Tests.``SqlCommand for querySetListN should``

open System.Data
open System.Data.Common
open Swensen.Unquote
open Xunit

open Vp.FSharp.Sql


let private toFieldName =
    sprintf "id%i"

let private boxes values =
    List.map box values

let private makeDbField index : Mocks.DbField' =
    { Name = toFieldName index
      FieldType = typeof<int>
      NativeTypeName = typeof<int>.Name
    }

let private readValueByIndex (columns: int list list) indexColumn _ (reader: SqlRecordReader<DbDataReader>) =
    columns.[indexColumn]
    |> List.map (reader.Value >> int)

[<Fact>]
let ``querySetList should open and close the connection when it's closed and access value by columnName`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[1..6] |> boxes |> List.splitInto 2
                 [1..8] |> boxes |> List.splitInto 2
                ]
                [[0..(0+2)] |> List.map makeDbField
                 [4..(4+3)] |> List.map makeDbField
                ]
    let columnsIndex = data.Columns |> List.map (List.mapi (fun i _ -> i))
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let! r0 =
                SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.querySetList connection (Mocks.makeDependencies None None)
                        (readValueByIndex columnsIndex 0)
        r0.Length =! 2
        r0 =! ([1..6] |> List.splitInto 2)
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``querySetList2 should open and close the connection when it's closed and access value by columnName`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[1..6] |> boxes |> List.splitInto 2
                 [1..8] |> boxes |> List.splitInto 2
                ]
                [[0..(0+2)] |> List.map makeDbField
                 [4..(4+3)] |> List.map makeDbField
                ]
    let columnsIndex = data.Columns |> List.map (List.mapi (fun i _ -> i))
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let! (r0, r1) =
                SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.querySetList2 connection (Mocks.makeDependencies None None)
                        (readValueByIndex columnsIndex 0)
                        (readValueByIndex columnsIndex 1)
        r0.Length =! 2
        r0 =! ([1..6] |> List.splitInto 2)
        r1.Length =! 2
        r1 =! ([1..8] |> List.splitInto 2)
        !openCall =! 1
        !closeCall =! 1
    }

[<Fact>]
let ``querySetList3 should open and close the connection when it's closed and access value by columnName`` () =
    let openCall = ref 0
    let closeCall = ref 0
    let openCallback () = incr openCall
    let closeCallback () = incr closeCall
    let data = Mocks.fakeData
                [[1..6] |> boxes |> List.splitInto 2
                 [1..8] |> boxes |> List.splitInto 2
                 [1..10] |> boxes |> List.splitInto 2
                ]
                [[0..(0+2)] |> List.map makeDbField
                 [4..(4+3)] |> List.map makeDbField
                 [8..(8+4)] |> List.map makeDbField
                ]
    let columnsIndex = data.Columns |> List.map (List.mapi (fun i _ -> i))
    async {
        use connection =
            Mocks.Reader (Mocks.makeReader data)
            |> Mocks.makeConnection "toto" ConnectionState.Closed openCallback closeCallback
        let! (r0, r1, r2) =
                SqlCommand.text "select 1"
                |> SqlCommand.noLogger
                |> SqlCommand.querySetList3 connection (Mocks.makeDependencies None None)
                        (readValueByIndex columnsIndex 0)
                        (readValueByIndex columnsIndex 1)
                        (readValueByIndex columnsIndex 2)
        r0.Length =! 2
        r0 =! ([1..6] |> List.splitInto 2)
        r1.Length =! 2
        r1 =! ([1..8] |> List.splitInto 2)
        r2.Length =! 2
        r2 =! ([1..10] |> List.splitInto 2)
        !openCall =! 1
        !closeCall =! 1
    }

