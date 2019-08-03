namespace Infrastructure
open Expecto
open FSharp.Control.Websockets


module Expect =
    let exceptionEquals exType message (ex : exn) =
        let actualExType = ex.GetType().ToString()
        if exType <> actualExType then
            Tests.failtestf "Expected exception of %s but got %s" exType actualExType
        if ex.Message <> message then
            Tests.failtestf "Expected message %s but got %s" message ex.Message

    let exceptionExists exType message (exns : exn seq) =
        let exnTypes =
            exns
            |> Seq.map (fun ex -> ex.GetType().ToString())
        let exnTypes = exnTypes |> Seq.distinct
        let typesFound = String.concat "; " exnTypes
        Expect.contains exnTypes exType  (sprintf "No exception matching that type found, only [%s]" typesFound)

        let exnMessages =
            exns
            |> Seq.map (fun ex -> ex.Message)
        Expect.contains exnMessages message  "No exception message matching that string found"
