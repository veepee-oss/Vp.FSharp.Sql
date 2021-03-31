module Vp.FSharp.Sql.Tests.Snippets

// begin-snippet: SqliteDbValue

/// Native SQLite DB types.
/// See https://www.sqlite.org/datatype3.html
type SqliteDbValue =
    | Null
    | Integer of int64
    | Real of double
    | Text of string
    | Blob of byte array

// end-snippet
