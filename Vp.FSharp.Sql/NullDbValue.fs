[<RequireQualifiedAccess>]
module Vp.FSharp.Sql.NullDbValue


/// Return the given DB null value if the given option is None, otherwise the underlying wrapped in Some.
let ifNone someDbValue noneDbValue = function
    | Some value -> someDbValue value
    | None -> noneDbValue

/// Return the given DB null value if the given option is Error, otherwise the underlying wrapped in Ok.
let ifError okDbValue errorDbValue = function
    | Ok value -> okDbValue value
    | Error error -> errorDbValue error
