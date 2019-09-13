``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14.5 (18F132) [Darwin 18.6.0]
Intel Core i7-4870HQ CPU 2.50GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100-preview7-012821
  [Host]    : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT DEBUG
  MediumRun : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT

Job=MediumRun  Runtime=Core  InvocationCount=1  
IterationCount=15  LaunchCount=2  UnrollFactor=1  
WarmupCount=10  

```
|                              Method | dataSize |        Mean |        Error |       StdDev |      Median | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------------------------ |--------- |------------:|-------------:|-------------:|------------:|------:|------:|------:|----------:|
|            **TaskReceiveMessageStream** |     **1000** |    **29.41 us** |     **6.116 us** |     **9.154 us** |    **26.66 us** |     **-** |     **-** |     **-** |   **17152 B** |
|           AsyncReceiveMessageStream |     1000 |    94.31 us |    11.986 us |    17.940 us |    89.90 us |     - |     - |     - |   18112 B |
|  ThreadSafeTaskReceiveMessageStream |     1000 |   101.03 us |    17.850 us |    25.600 us |    95.67 us |     - |     - |     - |     456 B |
| ThreadSafeAsyncReceiveMessageStream |     1000 |   109.97 us |    17.138 us |    25.121 us |   109.13 us |     - |     - |     - |    1048 B |
|            **TaskReceiveMessageStream** |    **20000** |   **108.20 us** |    **17.637 us** |    **26.398 us** |    **96.16 us** |     **-** |     **-** |     **-** |   **17472 B** |
|           AsyncReceiveMessageStream |    20000 |   254.09 us |    70.186 us |   100.659 us |   230.76 us |     - |     - |     - |   18112 B |
|  ThreadSafeTaskReceiveMessageStream |    20000 |   221.54 us |    24.578 us |    36.787 us |   213.88 us |     - |     - |     - |     456 B |
| ThreadSafeAsyncReceiveMessageStream |    20000 |   259.39 us |    51.557 us |    73.942 us |   219.59 us |     - |     - |     - |    1048 B |
|            **TaskReceiveMessageStream** |   **100000** |   **558.85 us** |   **124.849 us** |   **175.021 us** |   **567.27 us** |     **-** |     **-** |     **-** |   **19072 B** |
|           AsyncReceiveMessageStream |   100000 | 1,238.05 us |   606.065 us |   849.618 us | 1,011.49 us |     - |     - |     - |   18112 B |
|  ThreadSafeTaskReceiveMessageStream |   100000 | 1,895.92 us | 1,650.741 us | 2,470.752 us |   855.60 us |     - |     - |     - |     392 B |
| ThreadSafeAsyncReceiveMessageStream |   100000 |   872.36 us |   108.809 us |   162.861 us |   831.61 us |     - |     - |     - |    1048 B |
