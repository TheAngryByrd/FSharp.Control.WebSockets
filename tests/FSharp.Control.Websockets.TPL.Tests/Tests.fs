module Tests

open Expecto
open FSharp.Control.Websockets.TPL
open FSharp.Control.Tasks.V2

open System
open System.Net
open System.Net.WebSockets
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Configuration
open Infrastructure
open System.Threading
open Infrastructure


let testCaseTask name test = testCaseAsync name (test |> Async.AwaitTask)
let ftestCaseTask name test = ftestCaseAsync name (test |> Async.AwaitTask)
let ptestCaseTask name test = ptestCaseAsync name (test |> Async.AwaitTask)


let echoWebSocket (httpContext : HttpContext) (next : unit -> Task) = task {

    if httpContext.WebSockets.IsWebSocketRequest then
        let! (websocket : WebSocket) = httpContext.WebSockets.AcceptWebSocketAsync() |> Async.AwaitTask
        while websocket.State = WebSocketState.Open do
            try
                let! result =WebSocket.receiveMessageAsUTF8 websocket CancellationToken.None
                match result with
                | WebSocket.ReceiveUTF8Result.String text ->
                    do! WebSocket.sendMessageAsUTF8 websocket CancellationToken.None text
                | WebSocket.ReceiveUTF8Result.Closed (status, reason) ->
                    printfn "Socket closed %A - %s" status reason
            with e ->
                printfn "%A" e

        ()
    else
        do! next()

}

let configureEchoServer  (appBuilder : IApplicationBuilder) =
    appBuilder.UseWebSockets()
    |> Server.tuse (echoWebSocket)
    |> ignore

    ()

[<Tests>]
let tests =

    testList "Tests" [
        testCaseTask (sprintf "Full normal websocket interaction" ) <| task {
            use! wss = Server.getServerAndWs configureEchoServer
            use clientWebSocket = ThreadSafeWebSocket.createFromWebSocket (Dataflow.DataflowBlockOptions()) wss.clientWebSocket
            Expect.equal clientWebSocket.State WebSocketState.Open "Should be open"
            let expected = Generator.genStr 2000
            let! _ =  expected |> ThreadSafeWebSocket.sendMessageAsUTF8 clientWebSocket CancellationToken.None
            let! actual = ThreadSafeWebSocket.receiveMessageAsUTF8 clientWebSocket CancellationToken.None
            Expect.equal actual (Ok <| WebSocket.ReceiveUTF8Result.String expected) "did not echo"


            let! _ = ThreadSafeWebSocket.close clientWebSocket WebSocketCloseStatus.NormalClosure "Closing" CancellationToken.None
            Expect.equal clientWebSocket.State WebSocketState.Closed "Should be closed"
        }

        testCaseTask (sprintf "Full close output websocket interaction" ) <| task {
            use! wss = Server.getServerAndWs configureEchoServer
            use clientWebSocket = ThreadSafeWebSocket.createFromWebSocket (Dataflow.DataflowBlockOptions()) wss.clientWebSocket
            Expect.equal clientWebSocket.State WebSocketState.Open "Should be open"
            let expected = Generator.genStr 2000
            let! _ =  expected |> ThreadSafeWebSocket.sendMessageAsUTF8 clientWebSocket CancellationToken.None
            let! actual = ThreadSafeWebSocket.receiveMessageAsUTF8 clientWebSocket CancellationToken.None
            Expect.equal actual (Ok <| WebSocket.ReceiveUTF8Result.String expected) "did not echo"


            let! _ = ThreadSafeWebSocket.closeOutput clientWebSocket  WebSocketCloseStatus.NormalClosure "Closing" CancellationToken.None
            Expect.equal clientWebSocket.State WebSocketState.CloseSent "Should have sent closed without waiting acknowledgement"
        }
    ]
