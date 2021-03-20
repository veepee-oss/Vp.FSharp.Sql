module Vp.FSharp.Sql.Tests.Helpers

open Swensen.Unquote.Assertions

open Vp.FSharp.Sql


type FullCallCounter =
    { mutable OpenCall: int32
      mutable CloseCall: int32
      mutable ConnectionOpened: int32
      mutable ConnectionClosed: int32
      mutable CommandPrepared: int32
      mutable CommandExecuted: int32 }

[<RequireQualifiedAccess>]
module FullCallCounter =

    let incrOpenCall (callCounter: FullCallCounter) =
        callCounter.OpenCall <- callCounter.OpenCall + 1

    let incrCloseCall (callCounter: FullCallCounter) =
        callCounter.CloseCall <- callCounter.CloseCall + 1

    let incrConnectionOpened (callCounter: FullCallCounter) =
        callCounter.ConnectionOpened <- callCounter.ConnectionOpened + 1

    let incrConnectionClosed (callCounter: FullCallCounter) =
        callCounter.ConnectionClosed <- callCounter.ConnectionClosed + 1

    let incrCommandPrepared (callCounter: FullCallCounter) =
        callCounter.CommandPrepared <- callCounter.CommandPrepared + 1

    let incrCommandExecuted (callCounter: FullCallCounter) =
        callCounter.CommandExecuted <- callCounter.CommandExecuted + 1

    let createLoggerCallback callCounter =
        function
        | ConnectionOpened _ -> incrConnectionOpened callCounter
        | ConnectionClosed _ -> incrConnectionClosed callCounter
        | CommandPrepared _ -> incrCommandPrepared callCounter
        | CommandExecuted _ -> incrCommandExecuted callCounter
    let createOpenCallback (callCounter: FullCallCounter) = fun() -> incrOpenCall callCounter
    let createCloseCallback (callCounter: FullCallCounter) = fun() -> incrCloseCall callCounter
    let createCallbacks (callCounter: FullCallCounter) =
        (createOpenCallback callCounter, createCloseCallback callCounter, createLoggerCallback callCounter)

    let init openCall closeCall connectionOpened connectionClosed commandPrepared commandExecuted =
        { OpenCall = openCall
          CloseCall = closeCall
          ConnectionOpened = connectionOpened
          ConnectionClosed = connectionClosed
          CommandPrepared = commandPrepared
          CommandExecuted = commandExecuted }

    let initSame value = init value value value value value value

    let assertEqual
        (actual: FullCallCounter)
        expectedOpenCall
        expectedCloseCall
        expectedConnectionOpened
        expectedConnectionClosed
        expectedCommandPrepared
        expectedCommandExecuted =
        actual.OpenCall =! expectedOpenCall
        actual.CloseCall =! expectedCloseCall
        actual.ConnectionOpened =! expectedConnectionOpened
        actual.ConnectionClosed =! expectedConnectionClosed
        actual.CommandPrepared =! expectedCommandPrepared
        actual.CommandExecuted =! expectedCommandExecuted

type PartialCallCounter =
    { mutable OpenCall: int32
      mutable CloseCall: int32 }

[<RequireQualifiedAccess>]
module PartialCallCounter =
    let incrOpenCall (callCounter: PartialCallCounter) =
        callCounter.OpenCall <- callCounter.OpenCall + 1

    let incrCloseCall (callCounter: PartialCallCounter) =
        callCounter.CloseCall <- callCounter.CloseCall + 1

    let createOpenCallback (callCounter: PartialCallCounter) = fun() -> incrOpenCall callCounter
    let createCloseCallback (callCounter: PartialCallCounter) = fun() -> incrCloseCall callCounter
    let createCallbacks (callCounter: PartialCallCounter) =
        (createOpenCallback callCounter, createCloseCallback callCounter)

    let init openCall closeCall =
        { OpenCall = openCall
          CloseCall = closeCall }

    let initSame value = init value value

    let assertEqual
        (actual: PartialCallCounter)
        expectedOpenCall
        expectedCloseCall =
        actual.OpenCall =! expectedOpenCall
        actual.CloseCall =! expectedCloseCall
