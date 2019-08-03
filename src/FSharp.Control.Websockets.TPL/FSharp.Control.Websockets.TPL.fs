namespace FSharp.Control.Websockets.TPL
open System.Runtime.ExceptionServices
open System.Threading


module Stream =
    open System
    type System.IO.MemoryStream with

        /// **Description**
        ///
        /// Turns a string into a UTF8 MemoryStream
        ///
        /// **Parameters**
        ///   * `text` - parameter of type `string`
        ///
        /// **Output Type**
        ///   * `IO.MemoryStream`
        ///
        /// **Exceptions**
        ///
        static member UTF8toMemoryStream (text : string) =
            new IO.MemoryStream(Text.Encoding.UTF8.GetBytes text)

        /// **Description**
        ///
        /// Turns a `MemoryStream` into a a UTF8 string
        ///
        /// **Parameters**
        ///   * `stream` - parameter of type `IO.MemoryStream`
        ///
        /// **Output Type**
        ///   * `string`
        ///
        /// **Exceptions**
        ///
        static member ToUTF8String (stream : IO.MemoryStream) =
            stream.Seek(0L,IO.SeekOrigin.Begin) |> ignore //ensure start of stream
            stream.ToArray()
            |> Text.Encoding.UTF8.GetString
            |> fun s -> s.TrimEnd(char 0) // remove null teriminating characters

        /// **Description**
        ///
        /// Turns a `MemoryStream` into a a UTF8 string
        ///
        /// **Parameters**
        ///
        ///
        /// **Output Type**
        ///   * `string`
        ///
        /// **Exceptions**
        ///
        member stream.ToUTF8String () =
            stream |> System.IO.MemoryStream.ToUTF8String

module WebSocket =
    open Stream
    open System
    open System.Net.WebSockets
    open FSharp.Control.Tasks.V2

    /// **Description**
    ///
    /// Same as the `DefaultReceiveBufferSize` and `DefaultClientSendBufferSize` from the internal [WebSocketHelpers]( https://referencesource.microsoft.com/#System/net/System/Net/WebSockets/WebSocketHelpers.cs,285b8b64a4da6851).
    ///
    /// Current value: 16384
    ///
    /// **Output Type**
    ///   * `int`
    [<Literal>]
    let DefaultBufferSize  : int = 16384 // (16 * 1024)

    /// **Description**
    ///
    /// Determines if the websocket is open
    ///
    /// **Parameters**
    ///   * `socket` - parameter of type `WebSocket`
    ///
    /// **Output Type**
    ///   * `bool`
    ///
    /// **Exceptions**
    ///
    let isWebsocketOpen (socket : #WebSocket) =
        socket.State = WebSocketState.Open



    /// **Description**
    ///
    /// Receives data from the `System.Net.WebSockets.WebSocket` connection asynchronously.
    ///
    /// **Parameters**
    ///   * `websocket` - parameter of type `WebSocket`
    ///   * `buffer` - parameter of type `ArraySegment<byte>`- References the application buffer that is the storage location for the received data.
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///
    /// **Output Type**
    ///   * `Tasks.Task<WebSocketReceiveResult>` - An instance of this class represents the result of performing a single ReceiveAsync operation on a WebSocket.
    ///
    /// **Exceptions**
    ///
    let receive (websocket : WebSocket) (buffer : ArraySegment<byte>) (cancellationToken : CancellationToken) =
        websocket.ReceiveAsync(buffer, cancellationToken)



    /// **Description**
    ///
    ///  Sends data over the `System.Net.WebSockets.WebSocket` connection asynchronously.
    ///
    /// **Parameters**
    ///   * `buffer` - parameter of type `ArraySegment<byte>` - The buffer to be sent over the connection.
    ///   * `messageType` - parameter of type `WebSocketMessageType`- Indicates whether the application is sending a binary or text message.
    ///   * `endOfMessage` - parameter of type `bool` - Indicates whether the data in `buffer` is the last part of a message.
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///
    /// **Output Type**
    ///   * `Tasks.Task<unit>`
    ///
    /// **Exceptions**
    ///
    let send (websocket : WebSocket) (buffer : ArraySegment<byte>) (messageType : WebSocketMessageType) (endOfMessage : bool) (cancellationToken : CancellationToken) = task {
        return! websocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken)
    }


    /// **Description**
    ///
    /// Closes the WebSocket connection as an asynchronous operation using the close handshake defined in the http://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-06 section 7.
    ///
    /// **Parameters**
    ///   * `websocket` - parameter of type `WebSocket`
    ///   * `closeStatus` - parameter of type `WebSocketCloseStatus` - Indicates the reason for closing the WebSocket connection.
    ///   * `statusDescription` - parameter of type `string` -  Specifies a human readable explanation as to why the connection is closed.
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///
    /// **Output Type**
    ///   * `Tasks.Task<unit>`
    ///
    /// **Exceptions**
    ///
    let close (websocket : WebSocket) (closeStatus : WebSocketCloseStatus) (statusDescription: string) (cancellationToken: CancellationToken)  = task{
        return! websocket.CloseAsync(closeStatus, statusDescription, cancellationToken)
    }


    /// **Description**
    ///
    /// Initiates or completes the close handshake defined in the http://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-06.
    ///
    /// **Parameters**
    ///   * `websocket` - parameter of type `WebSocket`
    ///   * `closeStatus` - parameter of type `WebSocketCloseStatus` - Indicates the reason for closing the WebSocket connection.
    ///   * `statusDescription` - parameter of type `string`Specifies a human readable explanation as to why the connection is closed.
    ///   * `cancellationToken` - parameter of type `CancellationToken`  - Propagates the notification that operations should be canceled.
    ///
    /// **Output Type**
    ///   * `Tasks.Task<unit>`
    ///
    /// **Exceptions**
    ///
    let closeOutput (websocket : WebSocket)  (closeStatus : WebSocketCloseStatus) (statusDescription: string) (cancellationToken: CancellationToken)= task{
        return! websocket.CloseOutputAsync(closeStatus, statusDescription, cancellationToken)
    }

    /// Sends a whole message to the websocket read from the given stream

    /// **Description**
    ///
    /// Sends a whole message to the websocket read from the given stream
    ///
    /// **Parameters**
    ///   * `socket` - parameter of type `WebSocket`    ///   * `bufferSize` - parameter of type `int` - How many bytes to read from the stream at a time.  Recommended to use `DefaultBufferSize`.
    ///   * `messageType` - parameter of type `WebSocketMessageType` -  Indicates whether the application is sending a binary or text message.
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///   * `readableStream` - parameter of type `Stream` - A readable stream of data to send over the websocket connection.
    ///
    /// **Output Type**
    ///   * `Tasks.Task<unit>`
    ///
    /// **Exceptions**
    ///
    let sendMessage (socket : WebSocket) (bufferSize : int) (messageType : WebSocketMessageType) (cancellationToken : CancellationToken) (readableStream : #IO.Stream)  = task {
        let buffer = Array.create (bufferSize) Byte.MinValue
        let mutable moreToRead = true
        while moreToRead && isWebsocketOpen socket do
            let! read = readableStream.ReadAsync(buffer, 0, buffer.Length)
            if read > 0 then
                do!  send socket (ArraySegment(buffer |> Array.take read)) messageType false cancellationToken
            else
                moreToRead <- false
                do! send socket (ArraySegment(Array.empty)) messageType true cancellationToken
        }


    /// **Description**
    ///
    /// Sends a string as UTF8 over a websocket connection.
    ///
    /// **Parameters**
    ///   * `socket` - parameter of type `WebSocket`
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///   * `text` - parameter of type `string` - The string to send over the websocket.
    ///
    /// **Output Type**
    ///   * `Tasks.Task<unit>`
    ///
    /// **Exceptions**
    ///
    let sendMessageAsUTF8 (socket : WebSocket) (cancellationToken : CancellationToken) (text : string) = task {
        use stream = IO.MemoryStream.UTF8toMemoryStream text
        do! sendMessage socket DefaultBufferSize  WebSocketMessageType.Text cancellationToken stream
    }

    /// One of the possible results from reading a whole message from a websocket.
    type ReceiveStreamResult =
        /// Reading from the websocket completed
        | Stream of IO.Stream
        /// The websocket was closed during reading
        | Closed of closeStatus: WebSocketCloseStatus * closeStatusDescription:string



    /// **Description**
    ///
    /// Reads an entire message from a websocket.
    ///
    /// **Parameters**
    ///   * `socket` - parameter of type `WebSocket`
    ///   * `bufferSize` - parameter of type `int` - How many bytes to read from the socket at a time.  Recommended to use `DefaultBufferSize`.
    ///   * `messageType` - parameter of type `WebSocketMessageType` -  Indicates whether the application is receiving a binary or text message.
    ///   * `writeableStream` - parameter of type
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///   * `writeableStream` - parameter of type `IO.Stream` -  A writeable stream that data from the websocket is written into.
    ///
    /// **Output Type**
    ///   * `Tasks.Task<ReceiveStreamResult>`  - One of the possible results from reading a whole message from a websocket
    ///
    /// **Exceptions**
    ///
    let receiveMessage (socket : WebSocket) (bufferSize : int) (messageType : WebSocketMessageType) (cancellationToken : CancellationToken) (writeableStream : IO.Stream)  = task {
        let buffer = new ArraySegment<Byte>( Array.create (bufferSize) Byte.MinValue)
        let mutable moreToRead = true
        let mutable mainResult = Unchecked.defaultof<ReceiveStreamResult>
        while moreToRead do
            let! result  = receive socket buffer cancellationToken
            match result with
            | result when result.MessageType = WebSocketMessageType.Close || socket.State = WebSocketState.CloseReceived || socket.State = WebSocketState.CloseSent ->
                do! socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Close received", cancellationToken)
                moreToRead <- false
                mainResult <- Closed(socket.CloseStatus.Value, socket.CloseStatusDescription)
            | result ->
                if result.MessageType <> messageType then
                    failwithf "Invalid message type received %A, expected %A" result.MessageType messageType
                do! writeableStream.WriteAsync(buffer.Array, buffer.Offset, result.Count)
                if result.EndOfMessage then
                    moreToRead <- false
                    mainResult <- Stream writeableStream
        return mainResult
    }

    /// One of the possible results from reading a whole message from a websocket.
    type ReceiveUTF8Result =
        /// Reading from the websocket completed.
        | String of string
        /// The websocket was closed during reading.
        | Closed of closeStatus: WebSocketCloseStatus * closeStatusDescription:string




    /// **Description**
    ///
    /// Reads an entire message from a websocket as a string.
    ///
    /// **Parameters**
    ///   * `socket` - parameter of type `WebSocket`
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///
    /// **Output Type**
    ///   * `Tasks.Task<ReceiveUTF8Result>`
    ///
    /// **Exceptions**
    ///
    let receiveMessageAsUTF8 (socket : WebSocket) (cancellationToken : CancellationToken)  = task {
        use stream =  new IO.MemoryStream()
        let! result = receiveMessage socket  DefaultBufferSize WebSocketMessageType.Text cancellationToken stream
        match result with
        | ReceiveStreamResult.Stream s ->
            return stream |> IO.MemoryStream.ToUTF8String |> String
        | ReceiveStreamResult.Closed(status, reason) ->
            return ReceiveUTF8Result.Closed(status, reason)
    }



module ThreadSafeWebSocket =
    open System
    open System.Threading
    open System.Net.WebSockets
    open Stream
    open System.Threading.Tasks
    open System.Threading.Tasks.Dataflow
    open FSharp.Control.Tasks.V2

    type SendMessages =
    | Send of  bufferSize : CancellationToken * int * WebSocketMessageType *  IO.Stream * TaskCompletionSource<Result<unit, ExceptionDispatchInfo>>
    | Close of CancellationToken * WebSocketCloseStatus * string * TaskCompletionSource<Result<unit, ExceptionDispatchInfo>>
    | CloseOutput of CancellationToken * WebSocketCloseStatus * string * TaskCompletionSource<Result<unit, ExceptionDispatchInfo>>
    type ReceiveMessage = CancellationToken * int * WebSocketMessageType * IO.Stream  * TaskCompletionSource<Result<WebSocket.ReceiveStreamResult, ExceptionDispatchInfo>>

     /// The ThreadSafeWebSocket record allows applications to send and receive data after the WebSocket upgrade has completed.  This puts a `MailboxProcessor` in front of all send and receive messages to prevent multiple threads reading or writing to the socket at a time. Without this a websocket send/receive may throw a `InvalidOperationException` with the message:
     ///
     /// `There is already one outstanding 'SendAsync' call for this WebSocket instance. ReceiveAsync and SendAsync can be called simultaneously, but at most one outstanding operation for each of them is allowed at the same time.`
    type ThreadSafeWebSocket =
        { websocket : WebSocket
          sendChannel : BufferBlock<SendMessages>
          receiveChannel : BufferBlock<ReceiveMessage>
        }
        interface IDisposable with
            /// Used to clean up unmanaged resources for ASP.NET and self-hosted implementations.
            member x.Dispose() =
                x.websocket.Dispose()
        /// Returns the current state of the WebSocket connection.
        member x.State =
            x.websocket.State
        /// Indicates the reason why the remote endpoint initiated the close handshake.
        member x.CloseStatus =
            x.websocket.CloseStatus |> Option.ofNullable
        ///Allows the remote endpoint to describe the reason why the connection was closed.
        member x.CloseStatusDescription =
            x.websocket.CloseStatusDescription



    /// **Description**
    ///
    /// Creates a `ThreadSafeWebSocket` from an existing `WebSocket`.
    ///
    /// **Parameters**
    ///   * `webSocket` - parameter of type `WebSocket`
    ///
    /// **Output Type**
    ///   * `ThreadSafeWebSocket`
    ///
    /// **Exceptions**
    ///
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

        let sendLoop  = task {
            let mutable hasClosedBeenSent = false

            while webSocket |> WebSocket.isWebsocketOpen && not hasClosedBeenSent do
                let! message = sendBuffer.ReceiveAsync()
                match message with
                | Send (cancellationToken, buffer, messageType, stream, replyChannel) ->
                    do! wrap (fun () -> WebSocket.sendMessage webSocket  buffer messageType cancellationToken stream ) replyChannel
                | Close (cancellationToken, status, message, replyChannel) ->
                    hasClosedBeenSent <- true
                    do! wrap (fun ( ) -> WebSocket.close webSocket status message cancellationToken ) replyChannel
                | CloseOutput (cancellationToken, status, message, replyChannel) ->
                    hasClosedBeenSent <- true
                    do! wrap (fun () -> WebSocket.closeOutput webSocket status message cancellationToken ) replyChannel
        }

        let receiveLoop = task {
            while webSocket |> WebSocket.isWebsocketOpen do
                let! (cancellationToken, buffer, messageType, stream, replyChannel) = receiveBuffer.ReceiveAsync()
                do! wrap (fun () -> WebSocket.receiveMessage webSocket buffer messageType cancellationToken stream ) replyChannel
        }

        {
            websocket = webSocket
            sendChannel = sendBuffer
            receiveChannel = receiveBuffer
        }


    /// **Description**
    ///
    /// Sends a whole message to the websocket read from the given stream.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///   * `bufferSize` - parameter of type `int` - How many bytes to read from the stream at a time.  Recommended to use `DefaultBufferSize`.
    ///   * `messageType` - parameter of type `WebSocketMessageType` -  Indicates whether the application is sending a binary or text message.
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///   * `readableStream` - parameter of type `IO.Stream`
    ///
    /// **Output Type**
    ///   * `Task<Result<unit,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let sendMessage (threadSafeWebSocket : ThreadSafeWebSocket)  (bufferSize : int) (messageType : WebSocketMessageType) (cancellationToken : CancellationToken) (readableStream : #IO.Stream) = task {
        let reply = new TaskCompletionSource<_>()
        let msg = Send(cancellationToken,bufferSize, messageType, readableStream, reply)
        let! accepted = threadSafeWebSocket.sendChannel.SendAsync msg
        return! reply.Task
    }


    /// **Description**
    ///
    /// Sends a string as UTF8 over a websocket connection.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///   * `text` - parameter of type `string` - The string to send over the websocket.
    ///
    /// **Output Type**
    ///   * `Task<Result<unit,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let sendMessageAsUTF8 (threadSafeWebSocket : ThreadSafeWebSocket) (cancellationToken : CancellationToken) (text : string) = task {
        use stream = IO.MemoryStream.UTF8toMemoryStream text
        return! sendMessage threadSafeWebSocket  WebSocket.DefaultBufferSize WebSocketMessageType.Text cancellationToken stream
    }


    /// **Description**
    ///
    /// Reads an entire message from a websocket as a string.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
   ///   * `bufferSize` - parameter of type `int` - How many bytes to read from the socket at a time.  Recommended to use `DefaultBufferSize`.
    ///   * `messageType` - parameter of type `WebSocketMessageType` -  Indicates whether the application is sending a binary or text message.
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///   * `writeableStream` - parameter of type `IO.Stream` - A writeable stream that data from the websocket is written into.
    ///
    /// **Output Type**
    ///   * `Task<Result<WebSocket.ReceiveStreamResult,ExceptionDispatchInfo>>`- One of the possible results from reading a whole message from a websocket
    ///
    /// **Exceptions**
    ///
    let receiveMessage (threadSafeWebSocket : ThreadSafeWebSocket)  (bufferSize : int) (messageType : WebSocketMessageType) (cancellationToken : CancellationToken) (writeableStream : #IO.Stream) = task {
        let reply = new TaskCompletionSource<_>()
        let msg = (cancellationToken, bufferSize, messageType, writeableStream, reply)
        let! accepted = threadSafeWebSocket.receiveChannel.SendAsync(msg)
        return! reply.Task
    }


    /// **Description**
    ///
    /// Reads an entire message as a string.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///
    /// **Output Type**
    ///   * `Task<Result<WebSocket.ReceiveUTF8Result,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let receiveMessageAsUTF8 (threadSafeWebSocket : ThreadSafeWebSocket) (cancellationToken : CancellationToken) = task {
        use stream = new IO.MemoryStream()
        let! response = receiveMessage threadSafeWebSocket  WebSocket.DefaultBufferSize WebSocketMessageType.Text cancellationToken stream
        match response with
        | Ok (WebSocket.ReceiveStreamResult.Stream s) -> return stream |> IO.MemoryStream.ToUTF8String |> WebSocket.ReceiveUTF8Result.String |> Ok
        | Ok (WebSocket.ReceiveStreamResult.Closed(status, reason)) -> return WebSocket.ReceiveUTF8Result.Closed(status, reason) |> Ok
        | Error ex -> return Error ex

    }


    /// **Description**
    ///
    /// Closes the WebSocket connection as an asynchronous operation using the close handshake defined in the http://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-06 section 7.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///   * `closeStatus` - parameter of type `WebSocketCloseStatus` - Indicates the reason for closing the WebSocket connection.
    ///   * `statusDescription` - parameter of type `string` -  Specifies a human readable explanation as to why the connection is closed.
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///
    /// **Output Type**
    ///   * `Task<Result<unit,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let close (threadSafeWebSocket : ThreadSafeWebSocket)  (closeStatus : WebSocketCloseStatus) (statusDescription : string) (cancellationToken : CancellationToken) = task {
        let reply = new TaskCompletionSource<_>()
        let msg = Close(cancellationToken,closeStatus, statusDescription, reply)
        let! accepted = threadSafeWebSocket.sendChannel.SendAsync msg
        return! reply.Task
    }


    /// **Description**
    ///
    /// Closes the WebSocket connection as an asynchronous operation using the close handshake defined in the http://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-06 section 7.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
///   * `closeStatus` - parameter of type `WebSocketCloseStatus` - Indicates the reason for closing the WebSocket connection.
    ///   * `statusDescription` - parameter of type `string` -  Specifies a human readable explanation as to why the connection is closed.
    ///   * `cancellationToken` - parameter of type `CancellationToken` - Propagates the notification that operations should be canceled.
    ///
    /// **Output Type**
    ///   * `Task<Result<unit,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let closeOutput (threadSafeWebSocket : ThreadSafeWebSocket) (closeStatus : WebSocketCloseStatus) (statusDescription : string) (cancellationToken : CancellationToken) = task {
        let reply = new TaskCompletionSource<_>()
        let msg = CloseOutput(cancellationToken,closeStatus, statusDescription, reply)
        let! accepted = threadSafeWebSocket.sendChannel.SendAsync msg
        return! reply.Task
    }

