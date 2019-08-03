namespace Infrastructure

open System
open System.Net
open System.Net.WebSockets
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.TestHost
open Microsoft.Extensions.Configuration


module Generator =
    let random = Random(42)
    let genStr =
        let chars = "ABCDEFGHIJKLMNOPQRSTUVWUXYZ0123456789"
        let charsLen = chars.Length


        fun len ->
            let randomChars = [|for _ in 0..len -> chars.[random.Next(charsLen)]|]
            new string(randomChars)

module Server =


    // let juse (middlware : HttpContext -> (unit -> Job<unit>) -> Job<unit>) (app:IApplicationBuilder) =
    //     app.Use(
    //         Func<HttpContext,Func<Task>,Task>(
    //             fun env next ->
    //                 middlware env (next.Invoke >> Job.awaitUnitTask)
    //                 |> Hopac.startAsTask :> Task
    // ))

    let ause (middlware : HttpContext -> (unit -> Async<unit>) -> Async<unit>) (app:IApplicationBuilder) =
        app.Use(fun env next ->
                    middlware env (next.Invoke >> Async.AwaitTask)
                    |> Async.StartAsTask :> Task)

    let tuse (middlware : HttpContext -> (unit -> Task) -> Task<unit>) (app:IApplicationBuilder) =
        app.Use(fun env next ->
                    middlware env (next.Invoke)
                    :> Task)

    let constructLocalUri port =
        sprintf "http://127.0.0.1:%d" port

    let getKestrelServer configureServer uri = async {
        let configBuilder = new ConfigurationBuilder()
        let configBuilder = configBuilder.AddInMemoryCollection()
        let config = configBuilder.Build()
        config.["server.urls"] <- uri
        let host = WebHostBuilder()
                    .UseConfiguration(config)
                    .UseKestrel()
                    .Configure(fun app -> configureServer app )
                    .Build()

        do! host.StartAsync() |> Async.AwaitTask
        return host
    }

    let getOpenClientWebSocket (testServer : TestServer) = async {
        let ws = testServer.CreateWebSocketClient()
        let! ct = Async.CancellationToken
        return! ws.ConnectAsync(testServer.BaseAddress, ct) |> Async.AwaitTask
    }

    let getOpenWebSocket uri = async {
        let ws = new ClientWebSocket()
        let! ct = Async.CancellationToken
        do! ws.ConnectAsync(uri, ct) |> Async.AwaitTask
        return ws
    }

    // So we're able to tell the operating system to get a random free port by passing 0
    // and the system usually doesn't reuse a port until it has to
    // *pray*
    let getPort () =
        let listener = new Sockets.TcpListener(IPAddress.Loopback,0)
        listener.Start()
        let port  = (listener.LocalEndpoint :?> IPEndPoint).Port
        listener.Stop()
        port

    type WebSocketServer =
        { webhost : IWebHost
          clientWebSocket : ClientWebSocket }
        interface IDisposable with
            member x.Dispose() =
                x.clientWebSocket.Dispose()
                x.webhost.Dispose()

    let inline getServerAndWs configureServer = async {
        let uri = getPort () |> constructLocalUri
        let builder = UriBuilder(uri)
        builder.Scheme <- "ws"
        let! server = getKestrelServer configureServer uri
        let! clientWebSocket = builder.Uri |> getOpenWebSocket
        return {webhost = server; clientWebSocket = clientWebSocket}
    }
