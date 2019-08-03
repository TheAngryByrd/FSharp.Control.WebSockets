namespace FSharp.Control.Websockets.Benchmarks

module Main =

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

    let config =
         ManualConfig
                .Create(DefaultConfig.Instance)
                .With(Job.MediumRun.With(Runtime.Core))
                .With(MemoryDiagnoser.Default)
                .With(MarkdownExporter.GitHub)
                .With(ExecutionValidator.FailOnError)
                .With(MemoryDiagnoser.Default)

    let defaultSwitch () =
            Assembly.GetExecutingAssembly().GetTypes() |> Array.filter (fun t ->
                t.GetMethods ()|> Array.exists (fun m ->
                    m.GetCustomAttributes (typeof<BenchmarkAttribute>, false) <> [||] ))
            |> BenchmarkSwitcher


    [<EntryPoint>]
    let main argv =
        try
            defaultSwitch().Run(argv,config) |>ignore
        with e -> printfn "%A" e
        0 // return an integer exit code
