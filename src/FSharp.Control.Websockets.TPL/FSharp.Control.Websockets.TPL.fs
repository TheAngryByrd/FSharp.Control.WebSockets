namespace FSharp.Control.Websockets.TPL
open System.Runtime.ExceptionServices
open System.Threading


module Stream =
    open System
    type System.IO.MemoryStream with
        static member UTF8toMemoryStream (text : string) =
            new IO.MemoryStream(Text.Encoding.UTF8.GetBytes text)

        static member ToUTF8String (stream : IO.MemoryStream) =
            stream.Seek(0L,IO.SeekOrigin.Begin) |> ignore //ensure start of stream
            stream.ToArray()
            |> Text.Encoding.UTF8.GetString
            |> fun s -> s.TrimEnd(char 0) // remove null teriminating characters

        member stream.ToUTF8String () =
            stream |> System.IO.MemoryStream.ToUTF8String

module WebSocket =
    open Stream
    open System
    open System.Net.WebSockets
    open FSharp.Control.Tasks.V2

    /// **Description**
    /// (16 * 1024) = 16384
    /// https://referencesource.microsoft.com/#System/net/System/Net/WebSockets/WebSocketHelpers.cs,285b8b64a4da6851
    /// **Output Type**
    ///   * `int`
    [<Literal>]
    let DefaultBufferSize  : int = 16384 // (16 * 1024)

    let isWebsocketOpen (socket : #WebSocket) =
        socket.State = WebSocketState.Open

    let close (closeStatus : WebSocketCloseStatus) (statusDescription: string) (cancellationToken: CancellationToken) (ws : WebSocket) = task{
        return! ws.CloseAsync(closeStatus, statusDescription, cancellationToken)
    }

    let closeOutput (closeStatus : WebSocketCloseStatus) (statusDescription: string) (cancellationToken: CancellationToken) (ws : WebSocket) = task{
        return! ws.CloseOutputAsync(closeStatus, statusDescription, cancellationToken)
    }

    /// Sends a whole message to the websocket read from the given stream
    let sendMessage cancellationToken bufferSize messageType (readableStream : #IO.Stream) (socket : #WebSocket) = task {
        let buffer = Array.create (bufferSize) Byte.MinValue
        let mutable moreToRead = true
        while moreToRead && isWebsocketOpen socket do
            let! read = readableStream.ReadAsync(buffer, 0, buffer.Length)
            if read > 0 then
                do! socket.SendAsync(ArraySegment(buffer |> Array.take read), messageType, false, cancellationToken)
            else
                moreToRead <- false
                do! socket.SendAsync((ArraySegment(Array.empty)), messageType, true, cancellationToken)
        }

    let sendMessageAsUTF8 cancellationToken text socket = task {
        use stream = IO.MemoryStream.UTF8toMemoryStream text
        do! sendMessage cancellationToken DefaultBufferSize WebSocketMessageType.Text stream socket
    }

    type ReceiveStreamResult =
        | Stream of IO.Stream
        | StreamClosed of closeStatus: WebSocketCloseStatus * closeStatusDescription:string


    let receiveMessage cancellationToken bufferSize messageType (writeableStream : IO.Stream) (socket : WebSocket) = task {
        let buffer = new ArraySegment<Byte>( Array.create (bufferSize) Byte.MinValue)
        let mutable moreToRead = true
        let mutable mainResult = Unchecked.defaultof<ReceiveStreamResult>
        while moreToRead do
            let! result  = socket.ReceiveAsync(buffer, cancellationToken)
            match result with
            | result when result.MessageType = WebSocketMessageType.Close || socket.State = WebSocketState.CloseReceived || socket.State = WebSocketState.CloseSent ->
                do! socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close received", cancellationToken)
                moreToRead <- false
                mainResult <- StreamClosed(socket.CloseStatus.Value, socket.CloseStatusDescription)
            | result ->
                // printfn "result.MessageType -> %A" result.MessageType
                if result.MessageType <> messageType then
                    failwithf "Invalid message type received %A, expected %A" result.MessageType messageType
                do! writeableStream.WriteAsync(buffer.Array, buffer.Offset, result.Count)
                if result.EndOfMessage then
                    moreToRead <- false
                    mainResult <- Stream writeableStream
        return mainResult
    }
    type ReceiveUTF8Result =
        | String of string
        | StreamClosed of closeStatus: WebSocketCloseStatus * closeStatusDescription:string


    let receiveMessageAsUTF8 cancellationToken socket = task {
        use stream =  new IO.MemoryStream()
        let! result = receiveMessage cancellationToken DefaultBufferSize WebSocketMessageType.Text stream socket
        match result with
        | ReceiveStreamResult.Stream s ->
            return stream |> IO.MemoryStream.ToUTF8String |> String
        | ReceiveStreamResult.StreamClosed(status, reason) ->
            return ReceiveUTF8Result.StreamClosed(status, reason)
    }



module ThreadSafeWebSocket =
    open System
    open System.Threading
    open System.Net.WebSockets
    open Stream
    open System.Threading.Tasks
    open System.Threading.Tasks.Dataflow
    open FSharp.Control.Tasks.V2

    type MessageSendResult = Result<unit, ExceptionDispatchInfo>
    type MessageReceiveResult = Result<WebSocket.ReceiveStreamResult, ExceptionDispatchInfo>
    type SendMessages =
    | Send of  bufferSize : CancellationToken * int * WebSocketMessageType *  IO.Stream * TaskCompletionSource<MessageSendResult>
    | Close of CancellationToken * WebSocketCloseStatus * string * TaskCompletionSource<MessageSendResult>
    | CloseOutput of CancellationToken * WebSocketCloseStatus * string * TaskCompletionSource<MessageSendResult>
    type ReceiveMessage = CancellationToken * int * WebSocketMessageType * IO.Stream  * TaskCompletionSource<MessageReceiveResult>

    type ThreadSafeWebSocket =
        { websocket : WebSocket
          sendChannel : BufferBlock<SendMessages>
          receiveChannel : BufferBlock<ReceiveMessage>
        }
        interface IDisposable with
            member x.Dispose() =
                x.websocket.Dispose()

        member x.State =
            x.websocket.State

        member x.CloseStatus =
            x.websocket.CloseStatus |> Option.ofNullable

        member x.CloseStatusDescription =
            x.websocket.CloseStatusDescription


    let createFromWebSocket dataflowBlockOptions (webSocket : WebSocket) =
        let sendBuffer = BufferBlock<SendMessages>(dataflowBlockOptions)
        let receiveBuffer = BufferBlock<ReceiveMessage>(dataflowBlockOptions)

        /// handle executing a task in a try/catch and wrapping up the callstack info for later
        let inline wrap (action: unit -> Task<_>) (reply: TaskCompletionSource<_>) = task {
            try
                let! result = action ()
                reply.SetResult (Ok result)
            with
            | ex ->
                let dispatch = ExceptionDispatchInfo.Capture ex
                reply.SetResult(Error dispatch)
        }

        let sendLoop () = task {
            let mutable hasClosedBeenSent = false

            while webSocket |> WebSocket.isWebsocketOpen && not hasClosedBeenSent do
                let! message = sendBuffer.ReceiveAsync()
                match message with
                | Send (cancellationToken, buffer, messageType, stream, replyChannel) ->
                    do! wrap (fun () -> WebSocket.sendMessage cancellationToken buffer messageType stream webSocket) replyChannel
                | Close (cancellationToken, status, message, replyChannel) ->
                    hasClosedBeenSent <- true
                    do! wrap (fun ( ) -> WebSocket.close status message cancellationToken webSocket) replyChannel
                | CloseOutput (cancellationToken, status, message, replyChannel) ->
                    hasClosedBeenSent <- true
                    do! wrap (fun () -> WebSocket.closeOutput status message cancellationToken webSocket) replyChannel
        }

        let receiveLoop () = task {
            while webSocket |> WebSocket.isWebsocketOpen do
                let! (cancellationToken, buffer, messageType, stream, replyChannel) = receiveBuffer.ReceiveAsync()
                do! wrap (fun () -> WebSocket.receiveMessage cancellationToken buffer messageType stream webSocket) replyChannel
        }

        Task.Run<unit>(Func<Task<unit>>(sendLoop)) |> ignore
        Task.Run<unit>(Func<Task<unit>>(receiveLoop)) |> ignore

        {
            websocket = webSocket
            sendChannel = sendBuffer
            receiveChannel = receiveBuffer
        }

    let sendMessage (wsts : ThreadSafeWebSocket) cancellationToken bufferSize messageType stream = task {
        let reply = new TaskCompletionSource<_>()
        let msg = Send(cancellationToken,bufferSize, messageType, stream, reply)
        let! accepted = wsts.sendChannel.SendAsync msg
        return! reply.Task
    }

    let sendMessageAsUTF8(wsts : ThreadSafeWebSocket) cancellationToken (text : string) = task {
        use stream = IO.MemoryStream.UTF8toMemoryStream text
        return! sendMessage wsts cancellationToken WebSocket.DefaultBufferSize WebSocketMessageType.Text stream
    }

    let receiveMessage (wsts : ThreadSafeWebSocket) cancellationToken bufferSize messageType stream = task {
        let reply = new TaskCompletionSource<_>()
        let msg = (cancellationToken, bufferSize, messageType, stream, reply)
        let! accepted = wsts.receiveChannel.SendAsync(msg)
        return! reply.Task
    }

    let receiveMessageAsUTF8 (wsts : ThreadSafeWebSocket) cancellationToken = task {
        use stream = new IO.MemoryStream()
        let! response = receiveMessage wsts cancellationToken WebSocket.DefaultBufferSize WebSocketMessageType.Text stream
        match response with
        | Ok (WebSocket.ReceiveStreamResult.Stream s) -> return stream |> IO.MemoryStream.ToUTF8String |> WebSocket.ReceiveUTF8Result.String |> Ok
        | Ok (WebSocket.ReceiveStreamResult.StreamClosed(status, reason)) -> return WebSocket.ReceiveUTF8Result.StreamClosed(status, reason) |> Ok
        | Error ex -> return Error ex

    }

    let close (wsts : ThreadSafeWebSocket) cancellationToken status message = task {
        let reply = new TaskCompletionSource<_>()
        let msg = Close(cancellationToken,status, message, reply)
        let! accepted = wsts.sendChannel.SendAsync msg
        return! reply.Task
    }

    let closeOutput (wsts : ThreadSafeWebSocket) cancellationToken status message = task {
        let reply = new TaskCompletionSource<_>()
        let msg = CloseOutput(cancellationToken,status, message, reply)
        let! accepted = wsts.sendChannel.SendAsync msg
        return! reply.Task
    }

