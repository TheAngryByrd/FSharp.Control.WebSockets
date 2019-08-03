``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14.5 (18F132) [Darwin 18.6.0]
Intel Core i7-4870HQ CPU 2.50GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100-preview7-012821
  [Host]    : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT DEBUG
  MediumRun : .NET Core 2.2.5 (CoreCLR 4.6.27617.05, CoreFX 4.6.27618.01), 64bit RyuJIT

Job=MediumRun  Runtime=Core  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
|                              Method | dataSize |       Mean |      Error |     StdDev |    Gen 0 |    Gen 1 |    Gen 2 | Allocated |
|------------------------------------ |--------- |-----------:|-----------:|-----------:|---------:|---------:|---------:|----------:|
|                         **TaskReceive** |     **1000** |   **2.163 us** |  **0.0626 us** |  **0.0936 us** |   **0.0305** |        **-** |        **-** |     **168 B** |
|                        AsyncReceive |     1000 |  13.810 us |  1.1283 us |  1.6887 us |   0.3052 |        - |        - |    1175 B |
|            TaskReceiveMessageStream |     1000 |  17.400 us |  0.3206 us |  0.4598 us |   2.7466 |        - |        - |   17152 B |
|           AsyncReceiveMessageStream |     1000 |  56.880 us |  0.9333 us |  1.3385 us |   3.7231 |   0.1221 |        - |    1157 B |
|            TaskReceiveMessageString |     1000 |  18.430 us |  0.2183 us |  0.3268 us |   3.4485 |   0.0610 |        - |   21640 B |
|           AsyncReceiveMessageString |     1000 |  59.998 us |  0.9246 us |  1.3839 us |   4.2725 |   0.2441 |        - |    1142 B |
|  ThreadSafeTaskReceiveMessageString |     1000 |  56.214 us |  1.0608 us |  1.5878 us |   4.0894 |   0.0610 |        - |    1195 B |
| ThreadSafeAsyncReceiveMessageString |     1000 |  68.342 us |  1.4320 us |  2.1434 us |   4.6387 |   0.2441 |        - |    1150 B |
|                         **TaskReceive** |    **20000** |  **24.884 us** |  **0.3951 us** |  **0.5791 us** |   **0.0610** |        **-** |        **-** |     **168 B** |
|                        AsyncReceive |    20000 |  61.989 us |  1.6553 us |  2.3739 us |   0.3052 |        - |        - |    1213 B |
|            TaskReceiveMessageStream |    20000 |         NA |         NA |         NA |        - |        - |        - |         - |
|           AsyncReceiveMessageStream |    20000 |         NA |         NA |         NA |        - |        - |        - |         - |
|            TaskReceiveMessageString |    20000 |  90.623 us |  1.2651 us |  1.8935 us |  20.3857 |   4.0283 |        - |  127128 B |
|           AsyncReceiveMessageString |    20000 | 174.393 us |  5.9330 us |  8.8803 us |  21.7285 |   5.6152 |        - |    1144 B |
|  ThreadSafeTaskReceiveMessageString |    20000 | 156.837 us |  5.1408 us |  7.6945 us |  20.9961 |   5.1270 |        - |    1200 B |
| ThreadSafeAsyncReceiveMessageString |    20000 | 202.030 us |  4.0976 us |  5.7442 us |  22.7051 |   5.6152 |        - |    1176 B |
|                         **TaskReceive** |   **100000** | **114.547 us** |  **3.0905 us** |  **4.5301 us** |   **0.1221** |        **-** |        **-** |     **168 B** |
|                        AsyncReceive |   100000 | 171.131 us |  5.1504 us |  7.7089 us |   0.4883 |        - |        - |    1224 B |
|            TaskReceiveMessageStream |   100000 |         NA |         NA |         NA |        - |        - |        - |         - |
|           AsyncReceiveMessageStream |   100000 |         NA |         NA |         NA |        - |        - |        - |         - |
|            TaskReceiveMessageString |   100000 | 510.694 us |  9.2480 us | 13.5556 us | 133.3008 | 133.3008 | 133.3008 |  565384 B |
|           AsyncReceiveMessageString |   100000 | 718.110 us | 16.1913 us | 24.2343 us | 132.8125 | 132.8125 | 132.8125 |    1144 B |
|  ThreadSafeTaskReceiveMessageString |   100000 | 583.303 us |  6.5868 us |  9.4466 us | 147.4609 | 128.9063 | 125.0000 |    1774 B |
| ThreadSafeAsyncReceiveMessageString |   100000 | 720.041 us | 15.1943 us | 22.2716 us | 132.8125 | 132.8125 | 132.8125 |    1176 B |

Benchmarks with issues:
  ReceiveMarks.TaskReceiveMessageStream: MediumRun(Runtime=Core, IterationCount=15, LaunchCount=2, WarmupCount=10) [dataSize=20000]
  ReceiveMarks.AsyncReceiveMessageStream: MediumRun(Runtime=Core, IterationCount=15, LaunchCount=2, WarmupCount=10) [dataSize=20000]
  ReceiveMarks.TaskReceiveMessageStream: MediumRun(Runtime=Core, IterationCount=15, LaunchCount=2, WarmupCount=10) [dataSize=100000]
  ReceiveMarks.AsyncReceiveMessageStream: MediumRun(Runtime=Core, IterationCount=15, LaunchCount=2, WarmupCount=10) [dataSize=100000]
