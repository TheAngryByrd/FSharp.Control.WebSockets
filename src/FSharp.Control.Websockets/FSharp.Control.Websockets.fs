namespace FSharp.Control.Websockets

open System.Threading
open System.Threading.Tasks
open System.Runtime.ExceptionServices

type Async =

    /// **Description**
    ///
    /// Turn a function return a task that uses a cancelltionToken into an FSharp Async
    ///
    /// **Parameters**
    ///   * `f` - parameter of type `CancellationToken -> Task`
    ///
    /// **Output Type**
    ///   * `Async<unit>`
    ///
    /// **Exceptions**
    ///
    static member AwaitTaskWithCancellation (f: CancellationToken -> Task) : Async<unit> =
        async.Bind(Async.CancellationToken, f >> Async.AwaitTask)

    /// **Description**
    ///
    /// Turn a function return a task that uses a cancelltionToken into an FSharp Async
    ///
    /// **Parameters**
    ///   * `f` - parameter of type `CancellationToken -> Task<'a>`
    ///
    /// **Output Type**
    ///   * `Async<'a>`
    ///
    /// **Exceptions**
    ///
    static member AwaitTaskWithCancellation (f: CancellationToken -> Task<'a>) : Async<'a> =
        async.Bind(Async.CancellationToken, f >> Async.AwaitTask)

module Stream =
    open System
    open Microsoft

    let recyclableMemoryStreamManager = IO.RecyclableMemoryStreamManager()

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
            let bytes = Text.Encoding.UTF8.GetBytes text
            recyclableMemoryStreamManager.GetStream("UTF8toMemoryStream", bytes, 0, bytes.Length)


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
    let isWebsocketOpen (socket : WebSocket) =
        socket.State = WebSocketState.Open

    /// **Description**
    ///
    /// Receives data from the `System.Net.WebSockets.WebSocket` connection asynchronously.
    ///
    /// **Parameters**
    ///   * `websocket` - parameter of type `WebSocket`
    ///   * `buffer` - parameter of type `ArraySegment<byte>` - References the application buffer that is the storage location for the received data.
    ///
    /// **Output Type**
    ///   * `Async<WebSocketReceiveResult>` - An instance of this class represents the result of performing a single ReceiveAsync operation on a WebSocket.
    ///
    /// **Exceptions**
    ///
    let asyncReceive (websocket : WebSocket) (buffer : ArraySegment<byte>)  =
        fun ct ->  websocket.ReceiveAsync(buffer,ct)
        |> Async.AwaitTaskWithCancellation


    /// **Description**
    ///
    /// Sends data over the `System.Net.WebSockets.WebSocket` connection asynchronously.
    ///
    /// **Parameters**
    ///   * `websocket` - parameter of type `WebSocket`
    ///   * `buffer` - parameter of type `ArraySegment<byte>` - The buffer to be sent over the connection.
    ///   * `messageType` - parameter of type `WebSocketMessageType`- Indicates whether the application is sending a binary or text message.
    ///   * `endOfMessage` - parameter of type `bool` - Indicates whether the data in `buffer` is the last part of a message.
    ///
    /// **Output Type**
    ///   * `Async<unit>`
    ///
    /// **Exceptions**
    ///
    let asyncSend (websocket : WebSocket) (buffer : ArraySegment<byte>) (messageType : WebSocketMessageType) (endOfMessage : bool) =
        fun ct ->  websocket.SendAsync(buffer, messageType, endOfMessage, ct)
        |> Async.AwaitTaskWithCancellation


    /// **Description**
    ///
    /// Closes the WebSocket connection as an asynchronous operation using the close handshake defined in the http://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-06 section 7.
    ///
    /// **Parameters**
    ///   * `websocket` - parameter of type `WebSocket`
    ///   * `closeStatus` - parameter of type `WebSocketCloseStatus` - Indicates the reason for closing the WebSocket connection.
    ///   * `statusDescription` - parameter of type `string` -  Specifies a human readable explanation as to why the connection is closed.
    ///
    /// **Output Type**
    ///   * `Async<unit>`
    ///
    /// **Exceptions**
    ///
    let asyncClose (websocket : WebSocket) (closeStatus : WebSocketCloseStatus) (statusDescription : string)  =
        fun ct -> websocket.CloseAsync(closeStatus, statusDescription, ct)
        |> Async.AwaitTaskWithCancellation
        |> Async.Catch


    /// **Description**
    ///
    /// Initiates or completes the close handshake defined in the http://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-06.
    ///
    /// **Parameters**
    ///   * `websocket` - parameter of type `WebSocket`
    ///   * `closeStatus` - parameter of type `WebSocketCloseStatus` - Indicates the reason for closing the WebSocket connection.
    ///   * `statusDescription` - parameter of type `string`Specifies a human readable explanation as to why the connection is closed.
    ///
    /// **Output Type**
    ///   * `Async<unit>`
    ///
    /// **Exceptions**
    ///
    let asyncCloseOutput (websocket : WebSocket) (closeStatus : WebSocketCloseStatus) (statusDescription : string)  =
        fun ct -> websocket.CloseOutputAsync(closeStatus, statusDescription, ct)
        |> Async.AwaitTaskWithCancellation
        |> Async.Catch


    /// **Description**
    ///
    /// Sends a whole message to the websocket read from the given stream
    ///
    /// **Parameters**
    ///   * `socket` - parameter of type `WebSocket`
    ///   * `bufferSize` - parameter of type `int` - How many bytes to read from the stream at a time.  Recommended to use `DefaultBufferSize`.
    ///   * `messageType` - parameter of type `WebSocketMessageType` -  Indicates whether the application is sending a binary or text message.
    ///   * `readableStream` - parameter of type `Stream` - A readable stream of data to send over the websocket connection.
    ///
    /// **Output Type**
    ///   * `Async<unit>`
    ///
    /// **Exceptions**
    ///
    let sendMessage (socket : WebSocket) (bufferSize : int) (messageType : WebSocketMessageType) (readableStream : #IO.Stream)  = async {
        let buffer = Array.create (bufferSize) Byte.MinValue

        let rec sendMessage' () = async {
            if isWebsocketOpen socket then
                let! read = readableStream.AsyncRead(buffer, 0, buffer.Length)
                if read > 0 then
                    do! asyncSend socket (ArraySegment(buffer |> Array.take read))  messageType false
                    return! sendMessage'()
                else
                    do! (asyncSend socket (ArraySegment(Array.empty))  messageType true)
        }
        return! sendMessage'()
    }


    /// **Description**
    ///
    /// Sends a string as UTF8 over a websocket connection.
    ///
    /// **Parameters**
    ///   * `socket` - parameter of type `WebSocket`
    ///   * `text` - parameter of type `string` - The string to send over the websocket.
    ///
    /// **Output Type**
    ///   * `Async<unit>`
    ///
    /// **Exceptions**
    ///
    let sendMessageAsUTF8 (socket : WebSocket) (text : string) = async {
        use stream = IO.MemoryStream.UTF8toMemoryStream text
        return! sendMessage socket DefaultBufferSize WebSocketMessageType.Text stream
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
    ///   * `writeableStream` - parameter of type `IO.Stream` - A writeable stream that data from the websocket is written into.
    ///
    /// **Output Type**
    ///   * `Async<ReceiveStreamResult>` - One of the possible results from reading a whole message from a websocket
    ///
    /// **Exceptions**
    ///
    let receiveMessage (socket : WebSocket) (bufferSize : int) (messageType : WebSocketMessageType) (writeableStream : IO.Stream)  = async {
        let buffer = new ArraySegment<Byte>( Array.create (bufferSize) Byte.MinValue)

        let rec readTillEnd' () = async {
            let! result  = asyncReceive socket buffer
            match result with
            | result when result.MessageType = WebSocketMessageType.Close || socket.State = WebSocketState.CloseReceived || socket.State = WebSocketState.CloseSent ->
                // printfn "Close received! %A - %A" socket.CloseStatus socket.CloseStatusDescription
                let! _ = asyncCloseOutput socket WebSocketCloseStatus.NormalClosure "Close received by client"
                return ReceiveStreamResult.Closed(socket.CloseStatus.Value, socket.CloseStatusDescription)
            | result ->
                // printfn "result.MessageType -> %A" result.MessageType
                if result.MessageType <> messageType then return ()
                do! writeableStream.AsyncWrite(buffer.Array, 0, result.Count)
                if result.EndOfMessage then
                    return Stream writeableStream
                else
                    return! readTillEnd' ()
        }

        return! readTillEnd' ()

    }


    /// One of the possible results from reading a whole message from a websocket.
    type ReceiveUTF8Result =
        /// Reading from the websocket completed.
        | String of string
        /// The websocket was closed during reading.
        | Closed of closeStatus: WebSocketCloseStatus * closeStatusDescription:string


    /// **Description**
    ///
    /// Reads an entire message as a string.
    ///
    /// **Parameters**
    ///   * `socket` - parameter of type `WebSocket`
    ///
    /// **Output Type**
    ///   * `Async<ReceiveUTF8Result>`
    ///
    /// **Exceptions**
    ///
    let receiveMessageAsUTF8 (socket : WebSocket) = async {
        use stream =  recyclableMemoryStreamManager.GetStream()
        let! result = receiveMessage socket DefaultBufferSize WebSocketMessageType.Text stream
        match result with
        | ReceiveStreamResult.Stream s ->
            return stream |> IO.MemoryStream.ToUTF8String |> String |> Ok
        | ReceiveStreamResult.Closed(status, reason) ->
            return ReceiveUTF8Result.Closed(status, reason) |> Ok
    }

module ThreadSafeWebSocket =
    open System
    open System.Threading
    open System.Net.WebSockets
    open Stream

    type SendMessages =
    | Send of  bufferSize : int * WebSocketMessageType *  IO.Stream * AsyncReplyChannel<Result<unit, ExceptionDispatchInfo>>
    | Close of  WebSocketCloseStatus * string * AsyncReplyChannel<Result<unit, ExceptionDispatchInfo>>
    | CloseOutput of  WebSocketCloseStatus * string * AsyncReplyChannel<Result<unit, ExceptionDispatchInfo>>

    type ReceiveMessage =  int * WebSocketMessageType * IO.Stream  * AsyncReplyChannel<Result<WebSocket.ReceiveStreamResult, ExceptionDispatchInfo>>

    /// The ThreadSafeWebSocket record allows applications to send and receive data after the WebSocket upgrade has completed.  This puts a `MailboxProcessor` in front of all send and receive messages to prevent multiple threads reading or writing to the socket at a time. Without this a websocket send/receive may throw a `InvalidOperationException` with the message:
    ///
    /// `There is already one outstanding 'SendAsync' call for this WebSocket instance. ReceiveAsync and SendAsync can be called simultaneously, but at most one outstanding operation for each of them is allowed at the same time.`
    type ThreadSafeWebSocket =
        { websocket : WebSocket
          sendChannel : MailboxProcessor<SendMessages>
          receiveChannel : MailboxProcessor<ReceiveMessage>
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
    let createFromWebSocket (webSocket : WebSocket) =
        /// handle executing a task in a try/catch and wrapping up the callstack info for later
        let inline wrap (action: Async<Choice<_,exn>>) (reply: AsyncReplyChannel<_>) = async {
            match! action with
            | Choice1Of2 result -> reply.Reply(Ok result)
            | Choice2Of2 ex ->
                let dispatch = ExceptionDispatchInfo.Capture ex
                reply.Reply(Error dispatch)
        }

        let inline wrap' a = wrap (Async.Catch a)

        let sendAgent = MailboxProcessor<SendMessages>.Start(fun inbox ->
            let rec loop () = async {
                let! message = inbox.Receive()
                if webSocket |> WebSocket.isWebsocketOpen then
                    match message with
                    | Send (buffer, messageType, stream, replyChannel) ->
                        do! wrap' (WebSocket.sendMessage webSocket buffer messageType stream ) replyChannel
                        return! loop ()
                    | Close (status, message, replyChannel) ->
                        do! wrap (WebSocket.asyncClose webSocket status message) replyChannel
                    | CloseOutput (status, message, replyChannel) ->
                        do! wrap (WebSocket.asyncCloseOutput webSocket status message) replyChannel
            }
            loop ()
        )
        let receiveAgent = MailboxProcessor<ReceiveMessage>.Start(fun inbox ->
            let rec loop () = async {
                let! (buffer, messageType, stream, replyChannel) = inbox.Receive()
                if webSocket |> WebSocket.isWebsocketOpen then
                    do! wrap' (WebSocket.receiveMessage webSocket buffer messageType stream) replyChannel
                    return! loop ()
            }
            loop ()
        )
        {
            websocket = webSocket
            sendChannel = sendAgent
            receiveChannel = receiveAgent
        }




    /// **Description**
    ///
    /// Sends a whole message to the websocket read from the given stream.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///   * `bufferSize` - parameter of type `int` - How many bytes to read from the stream at a time.  Recommended to use `DefaultBufferSize`.
    ///   * `messageType` - parameter of type `WebSocketMessageType` -  Indicates whether the application is sending a binary or text message.
    ///   * `readableStream` - parameter of type `Stream` - A readable stream of data to send over the websocket connection.
    ///
    /// **Output Type**
    ///   * `Async<Result<unit,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let sendMessage (threadSafeWebSocket : ThreadSafeWebSocket) (bufferSize : int) (messageType : WebSocketMessageType) (readableStream : #IO.Stream) =
        threadSafeWebSocket.sendChannel.PostAndAsyncReply(fun reply -> Send(bufferSize, messageType, readableStream, reply))


    /// **Description**
    ///
    /// Sends a string as UTF8 over a websocket connection.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///   * `text` - parameter of type `string` - The string to send over the websocket.
    ///
    /// **Output Type**
    ///   * `Async<Result<unit,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let sendMessageAsUTF8(threadSafeWebSocket : ThreadSafeWebSocket) (text : string) = async {
        use stream = IO.MemoryStream.UTF8toMemoryStream text
        return! sendMessage threadSafeWebSocket  WebSocket.DefaultBufferSize  WebSocketMessageType.Text stream
    }


    /// **Description**
    ///
    /// Reads an entire message from a websocket as a string.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///   * `bufferSize` - parameter of type `int` - How many bytes to read from the socket at a time.  Recommended to use `DefaultBufferSize`.
    ///   * `messageType` - parameter of type `WebSocketMessageType` -  Indicates whether the application is sending a binary or text message.
    ///   * `writeableStream` - parameter of type `IO.Stream` - A writeable stream that data from the websocket is written into.
    ///
    /// **Output Type**
    ///   * `Async<Result<WebSocket.ReceiveStreamResult,ExceptionDispatchInfo>>` - One of the possible results from reading a whole message from a websocket
    ///
    /// **Exceptions**
    ///
    let receiveMessage (threadSafeWebSocket : ThreadSafeWebSocket) (bufferSize : int) (messageType : WebSocketMessageType) (writeableStream : IO.Stream) =
        threadSafeWebSocket.receiveChannel.PostAndAsyncReply(fun reply -> bufferSize, messageType, writeableStream, reply)


    /// **Description**
    ///
    /// Reads an entire message as a string.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///
    /// **Output Type**
    ///   * `Async<Result<WebSocket.ReceiveUTF8Result,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let receiveMessageAsUTF8 (threadSafeWebSocket : ThreadSafeWebSocket) = async {
        use stream = recyclableMemoryStreamManager.GetStream()
        let! response = receiveMessage threadSafeWebSocket WebSocket.DefaultBufferSize  WebSocketMessageType.Text stream
        match response with
        | Ok (WebSocket.ReceiveStreamResult.Stream s) ->
            return stream |> IO.MemoryStream.ToUTF8String |> WebSocket.ReceiveUTF8Result.String |> Ok
        | Ok (WebSocket.Closed(status, reason)) ->
            return Ok (WebSocket.ReceiveUTF8Result.Closed(status, reason))
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
    ///
    /// **Output Type**
    ///   * `Async<Result<unit,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let close (threadSafeWebSocket : ThreadSafeWebSocket)  (closeStatus : WebSocketCloseStatus) (statusDescription : string) =
        threadSafeWebSocket.sendChannel.PostAndAsyncReply(fun reply -> Close(closeStatus, statusDescription, reply))


    /// **Description**
    ///
    /// Initiates or completes the close handshake defined in the http://tools.ietf.org/html/draft-ietf-hybi-thewebsocketprotocol-06.
    ///
    /// **Parameters**
    ///   * `threadSafeWebSocket` - parameter of type `ThreadSafeWebSocket`
    ///   * `closeStatus` - parameter of type `WebSocketCloseStatus` - Indicates the reason for closing the WebSocket connection.
    ///   * `statusDescription` - parameter of type `string`Specifies a human readable explanation as to why the connection is closed.
    ///
    /// **Output Type**
    ///   * `Async<Result<unit,ExceptionDispatchInfo>>`
    ///
    /// **Exceptions**
    ///
    let closeOutput (threadSafeWebSocket : ThreadSafeWebSocket) (closeStatus : WebSocketCloseStatus) (statusDescription : string) =
        threadSafeWebSocket.sendChannel.PostAndAsyncReply(fun reply -> CloseOutput(closeStatus, statusDescription, reply))
