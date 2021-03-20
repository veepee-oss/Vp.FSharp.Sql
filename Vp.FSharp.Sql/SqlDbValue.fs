[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.NullDbValue


let ifNone someDbValue noneDbValue = function
    | Some value -> someDbValue value
    | None -> noneDbValue

let ifError okDbValue errorDbValue = function
    | Ok value -> okDbValue value
    | Error error -> errorDbValue error
