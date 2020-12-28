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
