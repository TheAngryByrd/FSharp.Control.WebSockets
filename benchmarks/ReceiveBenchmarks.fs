namespace FSharp.Control.Websockets.Benchmarks


module Setup =

    open System
    open Microsoft.AspNetCore.Builder
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.AspNetCore.Http
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open System.Net.WebSockets
    let sendRandomData dataSize (httpContext : HttpContext) (next : unit -> Task) = task {
        if httpContext.WebSockets.IsWebSocketRequest then
            let dataToSend =
                Infrastructure.Generator.genStr dataSize
                |> Text.UTF8Encoding.UTF8.GetBytes
                |> ArraySegment
            let! websocket = httpContext.WebSockets.AcceptWebSocketAsync()
            while websocket.State = WebSocketState.Open do
                do! websocket.SendAsync(dataToSend, WebSocketMessageType.Text,true, CancellationToken.None)
        else
            do! next ()
    }

    let createSendServer dataSize =
        let configure (appBuilder : IApplicationBuilder) =
            appBuilder.UseWebSockets()
            |> Infrastructure.Server.tuse (sendRandomData dataSize)
            |> ignore

            ()
        Infrastructure.Server.getServerAndWs configure

module Receive =
    open System
    open BenchmarkDotNet.Attributes
    open BenchmarkDotNet.Diagnosers
    open BenchmarkDotNet.Configs
    open BenchmarkDotNet.Jobs
    open BenchmarkDotNet.Running
    open BenchmarkDotNet.Validators
    open BenchmarkDotNet.Exporters
    open BenchmarkDotNet.Environments
    open BenchmarkDotNet.Extensions
    open System.Reflection
    open BenchmarkDotNet.Configs

    open FSharp.Control.Websockets
    open System.Net.WebSockets
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open System.Threading
    open System.Threading.Tasks

    type ReceiveMarks () =
        let mutable websocketServer = Unchecked.defaultof<Infrastructure.Server.WebSocketServer>
        let mutable buffer = Unchecked.defaultof<ArraySegment<Byte>>
        let mutable memoryStream = Unchecked.defaultof<IO.MemoryStream>

        let mutable threadSafeWebSocket = Unchecked.defaultof<ThreadSafeWebSocket.ThreadSafeWebSocket>
        let mutable threadSafeWebSocketTPL= Unchecked.defaultof<TPL.ThreadSafeWebSocket.ThreadSafeWebSocket>

        [<Params(
            1000, // Smaller than DefaultBufferSize
            20000, // Larger than DefaultBufferSize
            100000 // Large enough to be put on to Large Object Heap
            )>]
        member val public dataSize = 0 with get, set

        [<GlobalSetup>]
        member self.GlobalSetup() = task {
            memoryStream <- new IO.MemoryStream()
            let! server = Setup.createSendServer self.dataSize
            websocketServer <- server
            buffer <- ArraySegment<Byte>( Array.create (self.dataSize) Byte.MinValue)

            threadSafeWebSocket <- ThreadSafeWebSocket.createFromWebSocket websocketServer.clientWebSocket
            threadSafeWebSocketTPL <- TPL.ThreadSafeWebSocket.createFromWebSocket (Dataflow.DataflowBlockOptions()) websocketServer.clientWebSocket
        }

        [<GlobalCleanup>]
        member self.GlobalCleanup() = task {
            memoryStream.Dispose()
            (websocketServer :> IDisposable).Dispose()
        }

        [<Benchmark>]
        member this.TaskReceive () = task {
            return! TPL.WebSocket.receive websocketServer.clientWebSocket buffer CancellationToken.None
        }

        [<Benchmark>]
        member this.AsyncReceive () = task {
            return! WebSocket.asyncReceive websocketServer.clientWebSocket buffer
        }

        [<Benchmark>]
        member this.TaskReceiveMessageStream () = task {
            return! TPL.WebSocket.receiveMessage websocketServer.clientWebSocket WebSocket.DefaultBufferSize WebSocketMessageType.Text CancellationToken.None memoryStream
        }

        [<Benchmark>]
        member this.AsyncReceiveMessageStream () = task {
            return! WebSocket.receiveMessage websocketServer.clientWebSocket WebSocket.DefaultBufferSize WebSocketMessageType.Text memoryStream
        }

        [<Benchmark>]
        member this.TaskReceiveMessageString () = task {
            return! TPL.WebSocket.receiveMessageAsUTF8 websocketServer.clientWebSocket CancellationToken.None
        }

        [<Benchmark>]
        member this.AsyncReceiveMessageString () = task {
            return! WebSocket.receiveMessageAsUTF8 websocketServer.clientWebSocket
        }

        [<Benchmark>]
        member this.ThreadSafeTaskReceiveMessageString () = task {
            return! TPL.ThreadSafeWebSocket.receiveMessageAsUTF8 threadSafeWebSocketTPL CancellationToken.None
        }

        [<Benchmark>]
        member this.ThreadSafeAsyncReceiveMessageString () = task {
            return! ThreadSafeWebSocket.receiveMessageAsUTF8 threadSafeWebSocket
        }
