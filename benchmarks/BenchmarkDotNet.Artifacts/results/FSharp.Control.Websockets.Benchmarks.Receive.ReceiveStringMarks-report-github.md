``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14.5 (18F132) [Darwin 18.6.0]
Intel Core i7-4870HQ CPU 2.50GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100-preview7-012821
  [Host]    : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT DEBUG
  MediumRun : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT

Job=MediumRun  Runtime=Core  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
|                              Method | dataSize |      Mean |      Error |     StdDev |    Gen 0 |    Gen 1 |   Gen 2 | Allocated |
|------------------------------------ |--------- |----------:|-----------:|-----------:|---------:|---------:|--------:|----------:|
|            **TaskReceiveMessageString** |     **1000** |  **19.82 us** |  **0.5410 us** |  **0.7759 us** |   **3.3264** |   **0.0305** |       **-** |  **20.45 KB** |
|           AsyncReceiveMessageString |     1000 |  65.47 us |  2.2305 us |  3.3385 us |   4.1504 |   0.2441 |       - |  18.75 KB |
|  ThreadSafeTaskReceiveMessageString |     1000 |  70.94 us |  1.7102 us |  2.5598 us |   4.0283 |        - |       - |   1.32 KB |
| ThreadSafeAsyncReceiveMessageString |     1000 |  82.61 us |  1.5988 us |  2.3435 us |   4.5166 |   0.2441 |       - |   2.09 KB |
|            **TaskReceiveMessageString** |    **20000** |  **89.46 us** |  **1.5787 us** |  **2.3630 us** |  **12.5732** |   **1.4648** |       **-** |  **76.42 KB** |
|           AsyncReceiveMessageString |    20000 | 174.72 us |  7.1809 us | 10.5257 us |  14.6484 |   3.4180 |       - |  18.75 KB |
|  ThreadSafeTaskReceiveMessageString |    20000 | 157.10 us |  6.7310 us | 10.0747 us |  13.4277 |   2.4414 |       - |   1.32 KB |
| ThreadSafeAsyncReceiveMessageString |    20000 | 206.12 us |  3.3490 us |  5.0126 us |  14.4043 |   2.6855 |       - |   2.09 KB |
|            **TaskReceiveMessageString** |   **100000** | **463.29 us** |  **6.3242 us** |  **9.0699 us** | **416.0156** | **414.0625** | **72.2656** | **312.75 KB** |
|           AsyncReceiveMessageString |   100000 | 690.38 us | 22.8288 us | 32.7404 us | 407.2266 | 404.2969 | 72.2656 |   19.1 KB |
|  ThreadSafeTaskReceiveMessageString |   100000 | 498.12 us | 12.4942 us | 18.7008 us | 251.9531 | 246.0938 | 77.6367 |   1.73 KB |
| ThreadSafeAsyncReceiveMessageString |   100000 | 689.08 us | 25.7593 us | 38.5553 us | 334.9609 | 329.1016 | 75.1953 |   2.46 KB |
