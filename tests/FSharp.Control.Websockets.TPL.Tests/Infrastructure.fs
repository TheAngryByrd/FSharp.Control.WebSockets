namespace FSharp.Control.Websockets.Tests



module Expect =
    open Expecto
    let exceptionEquals exType message (ex : exn) =
        let actualExType = ex.GetType().ToString()
        if exType <> actualExType then
            Tests.failtestf "Expected exception of %s but got %s" exType actualExType
        if ex.Message <> message then
            Tests.failtestf "Expected message %s but got %s" message ex.Message

    let exceptionExists exType message (exns : exn seq) =
        let exnTypes =
            exns
            |> Seq.map(fun ex -> ex.GetType().ToString())
        Expect.contains exnTypes exType  "No exception matching that type found"

        let exnMessages =
            exns
            |> Seq.map(fun ex -> ex.Message)
        Expect.contains exnMessages message  "No exception message matching that string found"
