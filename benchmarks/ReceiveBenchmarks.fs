namespace FSharp.Control.Websockets.Benchmarks
open Microsoft.IO


module Setup =

    open System
    open Microsoft.AspNetCore.Builder
    open System.Threading
    open System.Threading.Tasks
    open Microsoft.AspNetCore.Http
    open System.Net.WebSockets
    // open System.Memory

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


module Formatters =
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

    let format = sprintf "%s\r\n%s\r\n"

    type FormatterMarks () =
        let mutable stringToWrite  = "Hello"

        [<Params(
            10, // Smaller
            100, // Larger
            1000, // Larger
            4096 // Largest buffer size
            )>]
        member val public dataSize = 0 with get, set

        [<GlobalSetup>]
        member self.GlobalSetup() =
            stringToWrite <-
                Infrastructure.Generator.genStr self.dataSize

        [<Benchmark>]
        member x.cachedsprintf () =
            format (stringToWrite.Length.ToString("X")) stringToWrite

        [<Benchmark>]
        member x.sprintfFormat () =
            sprintf "%s\r\n%s\r\n" (stringToWrite.Length.ToString("X")) stringToWrite

        [<Benchmark>]
        member x.stringFormatFormat () =
            String.Format("{0}\r\n{1}\r\n", (stringToWrite.Length.ToString("X")), stringToWrite)

        [<Benchmark>]
        member x.stringJoinFormat () =
            String.Join("", [|(stringToWrite.Length.ToString("X")); "\r\n"; stringToWrite; "\r\n"|])

        [<Benchmark>]
        member x.stringConcatFormat () =
            stringToWrite.Length.ToString("X") + "\r\n" + stringToWrite + "\r\n"

        [<Benchmark>]
        member x.stringBuilderFormat () =
            let sb = Text.StringBuilder()
            sb
                .Append(stringToWrite.Length.ToString("X"))
                .Append("\r\n")
                .Append(stringToWrite)
                .Append("\r\n")
                .ToString()

module UTF8Convertion =
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
    open System.Threading
    open System.Threading.Tasks
    open FSharp.Control.Websockets.Stream

    let ToUTF8String2 (stream : IO.MemoryStream) =
        stream.Seek(0L,IO.SeekOrigin.Begin) |> ignore //ensure start of stream
        use sr = new IO.StreamReader(stream, Text.Encoding.UTF8, false, 4096, true)
        sr.ReadToEnd()
        |> fun s -> s.TrimEnd(char 0) // remove null teriminating characters


    type UTF8Marks () =
        let mutable byteString = Unchecked.defaultof<byte array>
        let mutable memoryStream = Unchecked.defaultof<IO.MemoryStream>
        let recyclableMemoryStreamManager = RecyclableMemoryStreamManager()
        [<Params(
            1000, // Smaller than DefaultBufferSize
            20000, // Larger than DefaultBufferSize
            100000, // Large enough to be put on to Large Object Heap
            1000000, // Large enough to be put on to Large Object Heap
            10000000 // Large enough to be put on to Large Object Heap
            )>]
        member val public dataSize = 0 with get, set

        [<GlobalSetup>]
        member self.GlobalSetup() =
            byteString <-
                Infrastructure.Generator.genStr self.dataSize
                |> Text.Encoding.UTF8.GetBytes
            memoryStream <- recyclableMemoryStreamManager.GetStream()

        [<GlobalCleanup>]
        member self.GlobalCleanup() =
            memoryStream.Dispose()


        [<IterationSetup>]
        member self.IterationSetup() =
            memoryStream <- recyclableMemoryStreamManager.GetStream()
            memoryStream.Write(byteString, 0, byteString.Length)
            memoryStream.Seek(0L, IO.SeekOrigin.Begin) |> ignore

        [<IterationCleanup>]
        member self.IterationCleanup() =
            memoryStream.Dispose()

        [<Benchmark>]
        member this.UTFToBytes () =
            memoryStream.ToUTF8String()


        [<Benchmark>]
        member this.UTFToStreamReader () =
            ToUTF8String2 memoryStream

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
    open System.Threading
    open System.Threading.Tasks


    type ReceiveMarks () =
        let mutable websocketServer = Unchecked.defaultof<Infrastructure.Server.WebSocketServer>
        let mutable buffer = Unchecked.defaultof<ArraySegment<Byte>>


        [<Params(
            1000, // Smaller than DefaultBufferSize
            20000, // Larger than DefaultBufferSize
            100000 // Large enough to be put on to Large Object Heap
            )>]
        member val public dataSize = 0 with get, set

        [<GlobalSetup>]
        member self.GlobalSetup() =
            websocketServer <- Setup.createSendServer self.dataSize |> Async.RunSynchronously
            buffer <- ArraySegment<Byte>( Array.create (self.dataSize) Byte.MinValue)

        [<GlobalCleanup>]
        member self.GlobalCleanup() =
            (websocketServer :> IDisposable).Dispose()


        [<Benchmark>]
        member this.TaskReceive () =
            TPL.WebSocket.receive websocketServer.clientWebSocket buffer CancellationToken.None

        [<Benchmark>]
        member this.AsyncReceive () =
            WebSocket.asyncReceive websocketServer.clientWebSocket buffer
            |> Async.RunSynchronously


    type ReceiveStringMarks () =
        let mutable websocketServer = Unchecked.defaultof<Infrastructure.Server.WebSocketServer>

        let mutable threadSafeWebSocket = Unchecked.defaultof<ThreadSafeWebSocket.ThreadSafeWebSocket>
        let mutable threadSafeWebSocketTPL= Unchecked.defaultof<TPL.ThreadSafeWebSocket.ThreadSafeWebSocket>

        [<Params(
            1000, // Smaller than DefaultBufferSize
            20000, // Larger than DefaultBufferSize
            100000 // Large enough to be put on to Large Object Heap
            )>]
        member val public dataSize = 0 with get, set

        [<GlobalSetup>]
        member self.GlobalSetup() =
            websocketServer <- Setup.createSendServer self.dataSize |> Async.RunSynchronously

            threadSafeWebSocket <- ThreadSafeWebSocket.createFromWebSocket websocketServer.clientWebSocket
            threadSafeWebSocketTPL <- TPL.ThreadSafeWebSocket.createFromWebSocket (Dataflow.DataflowBlockOptions()) websocketServer.clientWebSocket


        [<GlobalCleanup>]
        member self.GlobalCleanup() =
            (websocketServer :> IDisposable).Dispose()

        [<Benchmark>]
        member this.TaskReceiveMessageString () =
            TPL.WebSocket.receiveMessageAsUTF8 websocketServer.clientWebSocket CancellationToken.None

        [<Benchmark>]
        member this.AsyncReceiveMessageString () =
            WebSocket.receiveMessageAsUTF8 websocketServer.clientWebSocket
            |> Async.RunSynchronously

        [<Benchmark>]
        member this.ThreadSafeTaskReceiveMessageString () =
            TPL.ThreadSafeWebSocket.receiveMessageAsUTF8 threadSafeWebSocketTPL CancellationToken.None

        [<Benchmark>]
        member this.ThreadSafeAsyncReceiveMessageString () =
            ThreadSafeWebSocket.receiveMessageAsUTF8 threadSafeWebSocket
            |> Async.RunSynchronously


    type ReceiveStreamMarks () =
        let recyclableMemoryStreamManager = RecyclableMemoryStreamManager()
        let mutable websocketServer = Unchecked.defaultof<Infrastructure.Server.WebSocketServer>
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
        member self.GlobalSetup() =
            websocketServer <- Setup.createSendServer self.dataSize |> Async.RunSynchronously
            memoryStream <- recyclableMemoryStreamManager.GetStream()

            threadSafeWebSocket <- ThreadSafeWebSocket.createFromWebSocket websocketServer.clientWebSocket
            threadSafeWebSocketTPL <- TPL.ThreadSafeWebSocket.createFromWebSocket (Dataflow.DataflowBlockOptions()) websocketServer.clientWebSocket


        [<GlobalCleanup>]
        member self.GlobalCleanup() =
            (websocketServer :> IDisposable).Dispose()

        [<IterationSetup>]
        member self.IterationSetup() =
            memoryStream <- recyclableMemoryStreamManager.GetStream()

        [<IterationCleanup>]
        member self.IterationCleanup() =
            memoryStream.Dispose()


        [<Benchmark>]
        member this.TaskReceiveMessageStream () =
            TPL.WebSocket.receiveMessage websocketServer.clientWebSocket WebSocket.DefaultBufferSize WebSocketMessageType.Text CancellationToken.None memoryStream

        [<Benchmark>]
        member this.AsyncReceiveMessageStream () =
            WebSocket.receiveMessage websocketServer.clientWebSocket WebSocket.DefaultBufferSize WebSocketMessageType.Text memoryStream
            |> Async.RunSynchronously

        [<Benchmark>]
        member this.ThreadSafeTaskReceiveMessageStream () =
            TPL.ThreadSafeWebSocket.receiveMessage threadSafeWebSocketTPL WebSocket.DefaultBufferSize WebSocketMessageType.Text CancellationToken.None memoryStream

        [<Benchmark>]
        member this.ThreadSafeAsyncReceiveMessageStream() =
            ThreadSafeWebSocket.receiveMessage threadSafeWebSocket WebSocket.DefaultBufferSize WebSocketMessageType.Text memoryStream
            |> Async.RunSynchronously
