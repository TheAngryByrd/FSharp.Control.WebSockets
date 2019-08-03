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
    open System.Reflection
    open BenchmarkDotNet.Configs

    open FSharp.Control.Websockets
    open System.Net.WebSockets
    open FSharp.Control.Tasks.V2.ContextInsensitive
    open System.Threading
    open System.Threading.Tasks

    type ReceiveMarks () =
        let mutable websocketServer = Unchecked.defaultof<Infrastructure.Server.WebSocketServer>

        [<Params(1, 15, 100, 1000, 16384, 65536)>]
        member val public dataSize = 0 with get, set
        [<Params(1, 15, 100, 1000)>]
        member val public rounds = 0 with get, set

        [<GlobalSetup>]
        member self.GlobalSetup() = task {
            let! server = Setup.createSendServer self.dataSize
            websocketServer <- server
        }

        [<GlobalCleanup>]
        member self.GlobalCleanup() = task {
            (websocketServer :> IDisposable).Dispose()
        }

        [<Benchmark>]
        member this.TaskReceive () = task {
            let buffer = new ArraySegment<Byte>( Array.create (this.dataSize) Byte.MinValue)
            for i=0 to this.rounds do
                let! receive = TPL.WebSocket.receive websocketServer.clientWebSocket buffer CancellationToken.None
                ()
        }

        [<Benchmark>]
        member this.AsyncReceive () = task {
            let buffer = new ArraySegment<Byte>( Array.create (this.dataSize) Byte.MinValue)
            for i=0 to this.rounds do
                let! receive = WebSocket.asyncReceive websocketServer.clientWebSocket buffer
                ()
        }
        [<Benchmark>]
        member this.TaskReceiveMessageStream () = task {
            use ms = new IO.MemoryStream()
            for i=0 to this.rounds do
                let! receive = TPL.WebSocket.receiveMessage websocketServer.clientWebSocket WebSocket.DefaultBufferSize WebSocketMessageType.Text CancellationToken.None ms
                ()
        }

        [<Benchmark>]
        member this.AsyncReceiveMessageStream () = task {
            use ms = new IO.MemoryStream()
            for i=0 to this.rounds do
                let! receive = WebSocket.receiveMessage websocketServer.clientWebSocket WebSocket.DefaultBufferSize WebSocketMessageType.Text ms
                ()
        }
        [<Benchmark>]
        member this.TaskReceiveMessageString () = task {
            for i=0 to this.rounds do
                let! receive = TPL.WebSocket.receiveMessageAsUTF8 websocketServer.clientWebSocket CancellationToken.None
                ()
        }

        [<Benchmark>]
        member this.AsyncReceiveMessageString () = task {
            for i=0 to this.rounds do
                let! receive = WebSocket.receiveMessageAsUTF8 websocketServer.clientWebSocket
                ()
        }

        [<Benchmark>]
        member this.ThreadSafeTaskReceiveMessageString () = task {
            let tsws = TPL.ThreadSafeWebSocket.createFromWebSocket (Dataflow.DataflowBlockOptions()) websocketServer.clientWebSocket
            for i=0 to this.rounds do
                let! receive = TPL.ThreadSafeWebSocket.receiveMessageAsUTF8 tsws CancellationToken.None
                ()
        }

        [<Benchmark>]
        member this.ThreadSafeAsyncReceiveMessageString () = task {
            let tsws = ThreadSafeWebSocket.createFromWebSocket websocketServer.clientWebSocket
            for i=0 to this.rounds do
                let! receive = ThreadSafeWebSocket.receiveMessageAsUTF8 tsws
                ()
        }
