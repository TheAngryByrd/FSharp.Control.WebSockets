namespace FSharp.Control.Websockets.TPL
open System.Runtime.ExceptionServices


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

module Websocket =
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
    let defaultBufferSize  : int = 16384 // (16 * 1024)

    let isWebsocketOpen (socket : #WebSocket) =
        socket.State = WebSocketState.Open

    /// Sends a whole message to the websocket read from the given stream
    let sendMessage cancellationToken bufferSize messageType (readableStream : #IO.Stream) (socket : #WebSocket) = task {
        let buffer = Array.create (bufferSize) Byte.MinValue
        let mutable moreToRead = true
        while moreToRead do
            let! read = readableStream.ReadAsync(buffer,0,buffer.Length)
            if read > 0 then
                do! socket.SendAsync(ArraySegment(buffer |> Array.take read),messageType, false, cancellationToken)
            else
                moreToRead <- false
                do! socket.SendAsync((ArraySegment(Array.empty)) ,messageType, true, cancellationToken)
        }

    let sendMessageAsUTF8 cancellationToken text socket = task {
        use stream = IO.MemoryStream.UTF8toMemoryStream text
        do! sendMessage cancellationToken defaultBufferSize WebSocketMessageType.Text stream socket
    }

    let receiveMessage cancellationToken bufferSize messageType (writeableStream : IO.Stream) (socket : WebSocket) = task {
        let buffer = new ArraySegment<Byte>( Array.create (bufferSize) Byte.MinValue)
        let mutable moreToRead = false
        while moreToRead do
            let! result  = socket.ReceiveAsync(buffer,cancellationToken)
            match result with
            | result when result.MessageType = WebSocketMessageType.Close || socket.State = WebSocketState.CloseReceived ->
                // printfn "Close received! %A - %A" socket.CloseStatus socket.CloseStatusDescription
                do! socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,  "Close received", cancellationToken)
            | result ->
                // printfn "result.MessageType -> %A" result.MessageType
                if result.MessageType <> messageType then
                    failwithf "Invalid message type received %A, expected %A" result.MessageType messageType
                do! writeableStream.WriteAsync(buffer.Array, buffer.Offset, result.Count)
                if result.EndOfMessage then
                    moreToRead <- false
    }


    let receiveMessageAsUTF8 cancellationToken socket = task {
        use stream =  new IO.MemoryStream()
        do! receiveMessage cancellationToken defaultBufferSize WebSocketMessageType.Text stream socket
        return stream |> IO.MemoryStream.ToUTF8String
    }



module ThreadSafeWebsocket =
    open System
    open System.Threading
    open System.Net.WebSockets
    open Stream
    open System.Threading.Tasks
    open System.Threading.Tasks.Dataflow
    open FSharp.Control.Tasks.V2
    
    type MessageResult = Result<unit, ExceptionDispatchInfo>
    type SendMessages =
    | Send of  bufferSize : CancellationToken * int * WebSocketMessageType *  IO.Stream * TaskCompletionSource<MessageResult>
    | Close of CancellationToken * WebSocketCloseStatus * string * TaskCompletionSource<MessageResult>
    | CloseOutput of CancellationToken * WebSocketCloseStatus * string * TaskCompletionSource<MessageResult>

    type ReceiveMessage = CancellationToken * int * WebSocketMessageType * IO.Stream  * TaskCompletionSource<MessageResult>

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
        let inline wrap (action: unit -> Task) (reply: TaskCompletionSource<_>) = task {
            try 
                do! action ()
                reply.SetResult (Ok ())
            with
            | ex -> 
                let dispatch = ExceptionDispatchInfo.Capture ex
                reply.SetResult(Error dispatch)
        }

        let sendLoop () = task {
            let mutable hasClosedBeenSent = false

            while webSocket |> Websocket.isWebsocketOpen && not hasClosedBeenSent do
                let! message = sendBuffer.ReceiveAsync()
                match message with
                | Send (cancellationToken, buffer, messageType, stream, replyChannel) ->
                    do! wrap (fun () -> Websocket.sendMessage cancellationToken buffer messageType stream webSocket :> _) replyChannel
                | Close (cancellationToken, status, message, replyChannel) ->
                    hasClosedBeenSent <- true
                    do! wrap (fun ( ) -> webSocket.CloseAsync(status,message,cancellationToken)) replyChannel
                | CloseOutput (cancellationToken, status, message, replyChannel) ->
                    hasClosedBeenSent <- true
                    do! wrap (fun () -> webSocket.CloseOutputAsync(status,message,cancellationToken)) replyChannel
        }

        let receiveLoop () = task {
            while webSocket |> Websocket.isWebsocketOpen do
                let! (cancellationToken, buffer, messageType, stream, replyChannel) = receiveBuffer.ReceiveAsync()
                do! wrap (fun () -> Websocket.receiveMessage cancellationToken buffer messageType stream webSocket :> _) replyChannel
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
        return! sendMessage wsts cancellationToken Websocket.defaultBufferSize WebSocketMessageType.Text stream
    }

    let receiveMessage (wsts : ThreadSafeWebSocket) cancellationToken bufferSize messageType stream = task {
        let reply = new TaskCompletionSource<_>()
        let msg = (cancellationToken, bufferSize, messageType, stream, reply)
        let! accepted = wsts.receiveChannel.SendAsync(msg)
        return! reply.Task
    }

    let receiveMessageAsUTF8 (wsts : ThreadSafeWebSocket) cancellationToken = task {
        use stream = new IO.MemoryStream()
        let! response = receiveMessage wsts cancellationToken Websocket.defaultBufferSize WebSocketMessageType.Text stream
        match response with
        | Ok () -> return stream |> IO.MemoryStream.ToUTF8String |> Ok
        | Error ex -> return Error ex
    }

    let close (wsts : ThreadSafeWebSocket) cancellationToken status message = task {
        let reply = new TaskCompletionSource<_>()
        let msg = Close(cancellationToken,status, message, reply)
        let! accepted = wsts.sendChannel.SendAsync msg
        return! reply.Task
    }

    let closeOutput (wsts : ThreadSafeWebSocket) cancellationToken status message =task {
        let reply = new TaskCompletionSource<_>()
        let msg = CloseOutput(cancellationToken,status, message, reply)
        let! accepted = wsts.sendChannel.SendAsync msg
        return! reply.Task
    }

