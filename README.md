# FSharp.Control.Websockets

FSharp.Control.WebSockets wraps [dotnet websockets](https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.websocket?view=netcore-2.0) in FSharp friendly functions and has a ThreadSafe version.


### Why? 


#### Thread safety

Dotnet websockets only allow for one receive and one send at a time. If multiple threads try to write to a websocket, it will throw a `System.InvalidOperationException` with the message `There is already one outstanding 'SendAsync' call for this WebSocket instance. ReceiveAsync and SendAsync can be called simultaneously, but at most one outstanding operation for each of them is allowed at the same time.`. This wraps a websocket in a FIFO that allows for multiple threads to write or read at the same time. See [Websocket Remarks](https://docs.microsoft.com/en-us/dotnet/api/system.net.websockets.websocket.sendasync?view=netcore-2.0#Remarks) on Microsoft for more information.

#### F# friendly and correct behavior

This provides a `readMessage` type function. This is the biggest stumbling block people have when working with websockets. You have to keep reading from the message until it’s finished.  People either don’t and end up having corrupted messages with a small buffer or have a buffer that is giant and can be a memory hog for smaller messages.

#### Memory usage 

Uses [RecyclableMemoryStreamManager](https://www.philosophicalgeek.com/2015/02/06/announcing-microsoft-io-recycablememorystream/) and [ArrayPool](https://docs.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1?view=netstandard-2.1) to help keep memory usage and GC down.

---

## Builds

MacOS/Linux | Windows
--- | ---
[![Travis Badge](https://travis-ci.org/TheAngryByrd/FSharp.Control.Websockets.svg?branch=master)](https://travis-ci.org/TheAngryByrd/FSharp.Control.Websockets) | [![Build status](https://ci.appveyor.com/api/projects/status/github/TheAngryByrd/fsharp-control-websockets?svg=true)](https://ci.appveyor.com/project/TheAngryByrd/fsharp-control-websockets)
[![Build History](https://buildstats.info/travisci/chart/TheAngryByrd/FSharp.Control.Websockets)](https://travis-ci.org/TheAngryByrd/FSharp.Control.Websockets/builds) | [![Build History](https://buildstats.info/appveyor/chart/TheAngryByrd/fsharp-control-websockets)](https://ci.appveyor.com/project/TheAngryByrd/fsharp-control-websockets)  


## Nuget 

Name | Stable | Prerelease
--- | --- | ---
FSharp.Control.Websockets | [![NuGet Badge](https://buildstats.info/nuget/FSharp.Control.Websockets)](https://www.nuget.org/packages/FSharp.Control.Websockets/) | [![NuGet Badge](https://buildstats.info/nuget/FSharp.Control.Websockets?includePreReleases=true)](https://www.nuget.org/packages/FSharp.Control.Websockets/)
FSharp.Control.Websockets.TPL | [![NuGet Badge](https://buildstats.info/nuget/FSharp.Control.Websockets.TPL)](https://www.nuget.org/packages/FSharp.Control.Websockets.TPL/) | [![NuGet Badge](https://buildstats.info/nuget/FSharp.Control.Websockets.TPL?includePreReleases=true)](https://www.nuget.org/packages/FSharp.Control.Websockets.TPL/)


### Using

```fsharp
open System
open System.Net.WebSockets
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open FSharp.Control.Websockets
open Microsoft.Extensions.Configuration

let echoWebSocket (httpContext: HttpContext) (next: unit -> Async<unit>) =
  async {
    if httpContext.WebSockets.IsWebSocketRequest then
      let! websocket =
        httpContext.WebSockets.AcceptWebSocketAsync()
        |> Async.AwaitTask
      // Create a thread-safe WebSocket from an existing websocket
      let threadSafeWebSocket =
        ThreadSafeWebSocket.createFromWebSocket websocket

      while threadSafeWebSocket.State = WebSocketState.Open do
        try
          let! result =
            threadSafeWebSocket
            |> ThreadSafeWebSocket.receiveMessageAsUTF8

          match result with
          | Ok (WebSocket.ReceiveUTF8Result.String text) ->
              //Echo it back to the client
              do! WebSocket.sendMessageAsUTF8 websocket (text)
          | Ok (WebSocket.ReceiveUTF8Result.Closed (status, reason)) -> printfn "Socket closed %A - %s" status reason
          | Error (ex) -> printfn "Receiving threw an exception %A" ex.SourceException
        with e -> printfn "%A" e

    else
      do! next ()
  }

//Convenience function for making middleware with F# asyncs and funcs
let fuse (middlware: HttpContext -> (unit -> Async<unit>) -> Async<unit>) (app: IApplicationBuilder) =
  app.Use
    (fun env next ->
      middlware env (next.Invoke >> Async.AwaitTask)
      |> Async.StartAsTask
      :> Task)


let configureEchoServer (appBuilder: IApplicationBuilder) =
  appBuilder.UseWebSockets()
  |> fuse (echoWebSocket)
  |> ignore

let getKestrelServer configureServer uri =
  let configBuilder = new ConfigurationBuilder()
  let configBuilder = configBuilder.AddInMemoryCollection()
  let config = configBuilder.Build()
  config.["server.urls"] <- uri

  let host =
    WebHostBuilder()
      .UseConfiguration(config)
      .UseKestrel()
      .Configure(fun app -> configureServer app)
      .Build()
      .Start()

  host

[<EntryPoint>]
let main argv =
  getKestrelServer configureEchoServer "http://localhost:3000"
  Console.ReadKey() |> ignore

  0
```

---


### Building


Make sure the following **requirements** are installed in your system:

* [dotnet SDK](https://www.microsoft.com/net/download/core) 2.0 or higher
* [Mono](http://www.mono-project.com/) if you're on Linux or macOS.

```
> build.cmd // on windows
$ ./build.sh  // on unix
```

#### Environment Variables

* `CONFIGURATION` will set the [configuration](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-build?tabs=netcore2x#options) of the dotnet commands.  If not set it will default to Release.
  * `CONFIGURATION=Debug ./build.sh` will result in things like `dotnet build -c Debug`
* `GITHUB_TOKEN` will be used to upload release notes and nuget packages to github.
  * Be sure to set this before releasing

### Watch Tests

The `WatchTests` target will use [dotnet-watch](https://github.com/aspnet/Docs/blob/master/aspnetcore/tutorials/dotnet-watch.md) to watch for changes in your lib or tests and re-run your tests on all `TargetFrameworks`

```
./build.sh WatchTests
```

### Releasing
* [Start a git repo with a remote](https://help.github.com/articles/adding-an-existing-project-to-github-using-the-command-line/)

```
git add .
git commit -m "Scaffold"
git remote add origin origin https://github.com/user/MyCoolNewLib.git
git push -u origin master
```

* [Add your nuget API key to paket](https://fsprojects.github.io/Paket/paket-config.html#Adding-a-NuGet-API-key)

```
paket config add-token "https://www.nuget.org" 4003d786-cc37-4004-bfdf-c4f3e8ef9b3a
```

* [Create a GitHub OAuth Token](https://help.github.com/articles/creating-a-personal-access-token-for-the-command-line/)
    * You can then set the `GITHUB_TOKEN` to upload release notes and artifacts to github
    * Otherwise it will fallback to username/password


* Then update the `RELEASE_NOTES.md` with a new version, date, and release notes [ReleaseNotesHelper](https://fsharp.github.io/FAKE/apidocs/fake-releasenoteshelper.html)

```
#### 0.2.0 - 2017-04-20
* FEATURE: Does cool stuff!
* BUGFIX: Fixes that silly oversight
```

* You can then use the `Release` target.  This will:
    * make a commit bumping the version:  `Bump version to 0.2.0` and add the release notes to the commit
    * publish the package to nuget
    * push a git tag

```
./build.sh Release
```


### Code formatting

To format code run the following target

```
./build.sh FormatCode
```

This uses [Fantomas](https://github.com/fsprojects/fantomas) to do code formatting.  Please report code formatting bugs to that repository.
