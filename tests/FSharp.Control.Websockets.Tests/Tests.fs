module Tests

open Expecto
open FSharp.Control.Websockets

open Infrastructure
open System
open System.Net
open System.Net.WebSockets
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http


open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Configuration

let echoWebSocket (httpContext: HttpContext) (next: unit -> Async<unit>) = async {

    if httpContext.WebSockets.IsWebSocketRequest then
        let! (websocket: WebSocket) =
            httpContext.WebSockets.AcceptWebSocketAsync()
            |> Async.AwaitTask

        while websocket.State = WebSocketState.Open do
            try
                let! result = WebSocket.receiveMessageAsUTF8 websocket

                match result with
                | Ok (WebSocket.ReceiveUTF8Result.String text) ->
                    do! WebSocket.sendMessageAsUTF8 websocket text
                | Ok (WebSocket.ReceiveUTF8Result.Closed (status, reason)) ->
                    printfn "Socket closed %A - %s" status reason
                | Error e -> raise e
            with e ->
                printfn "%A" e

        ()
    else
        do! next ()

}

let configureEchoServer (appBuilder: IApplicationBuilder) =
    appBuilder.UseWebSockets()
    |> Server.ause (echoWebSocket)
    |> ignore

    ()

[<Tests>]
let tests =

    testList "Tests" [
        testCaseAsync (sprintf "Full normal websocket interaction")
        <| async {
            use! wss = Server.getServerAndWs configureEchoServer
            use clientWebSocket = ThreadSafeWebSocket.createFromWebSocket wss.clientWebSocket
            Expect.equal clientWebSocket.State WebSocketState.Open "Should be open"
            let expected = Generator.genStr 2000

            let! _ =
                expected
                |> ThreadSafeWebSocket.sendMessageAsUTF8 clientWebSocket

            let! actual =
                clientWebSocket
                |> ThreadSafeWebSocket.receiveMessageAsUTF8

            Expect.equal
                actual
                (Ok
                 <| WebSocket.ReceiveUTF8Result.String expected)
                "did not echo"


            let! _ =
                ThreadSafeWebSocket.close
                    clientWebSocket
                    WebSocketCloseStatus.NormalClosure
                    "Closing"

            Expect.equal clientWebSocket.State WebSocketState.Closed "Should be closed"
        }

        testCaseAsync (sprintf "Full close output websocket interaction")
        <| async {
            use! wss = Server.getServerAndWs configureEchoServer
            use clientWebSocket = ThreadSafeWebSocket.createFromWebSocket wss.clientWebSocket
            Expect.equal clientWebSocket.State WebSocketState.Open "Should be open"
            let expected = Generator.genStr 2000

            let! _ =
                expected
                |> ThreadSafeWebSocket.sendMessageAsUTF8 clientWebSocket

            let! actual =
                clientWebSocket
                |> ThreadSafeWebSocket.receiveMessageAsUTF8

            Expect.equal
                actual
                (Ok
                 <| WebSocket.ReceiveUTF8Result.String expected)
                "did not echo"


            let! _ =
                ThreadSafeWebSocket.closeOutput
                    clientWebSocket
                    WebSocketCloseStatus.NormalClosure
                    "Closing"

            Expect.equal
                clientWebSocket.State
                WebSocketState.CloseSent
                "Should have sent closed without waiting acknowledgement"
        }
    ]
