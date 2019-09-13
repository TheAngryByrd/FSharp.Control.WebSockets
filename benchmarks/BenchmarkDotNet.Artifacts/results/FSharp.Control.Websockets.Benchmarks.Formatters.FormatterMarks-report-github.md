``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14.5 (18F132) [Darwin 18.6.0]
Intel Core i7-4870HQ CPU 2.50GHz (Haswell), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100-preview7-012821
  [Host]    : .NET Core 3.0.0-preview7-27912-14 (CoreCLR 4.700.19.32702, CoreFX 4.700.19.36209), 64bit RyuJIT DEBUG
  MediumRun : .NET Core 3.0.0-preview7-27912-14 (CoreCLR 4.700.19.32702, CoreFX 4.700.19.36209), 64bit RyuJIT

Job=MediumRun  Runtime=Core  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
|              Method | dataSize |        Mean |      Error |     StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------- |--------- |------------:|-----------:|-----------:|-------:|------:|------:|----------:|
|       **cachedsprintf** |       **10** |   **164.96 ns** |   **3.729 ns** |   **5.581 ns** | **0.0842** |     **-** |     **-** |     **264 B** |
|       sprintfFormat |       10 |   238.95 ns |   8.749 ns |  12.824 ns | 0.1173 |     - |     - |     368 B |
|  stringFormatFormat |       10 |   170.98 ns |   2.360 ns |   3.532 ns | 0.0253 |     - |     - |      80 B |
|    stringJoinFormat |       10 |   126.75 ns |   4.003 ns |   5.741 ns | 0.0432 |     - |     - |     136 B |
|  stringConcatFormat |       10 |    77.20 ns |   1.864 ns |   2.733 ns | 0.0254 |     - |     - |      80 B |
| stringBuilderFormat |       10 |   102.49 ns |   4.549 ns |   6.378 ns | 0.0587 |     - |     - |     184 B |
|       **cachedsprintf** |      **100** |   **256.75 ns** |  **37.865 ns** |  **56.674 ns** | **0.1452** |     **-** |     **-** |     **456 B** |
|       sprintfFormat |      100 |   270.25 ns |  19.719 ns |  28.280 ns | 0.1783 |     - |     - |     560 B |
|  stringFormatFormat |      100 |   195.49 ns |   5.717 ns |   8.557 ns | 0.0865 |     - |     - |     272 B |
|    stringJoinFormat |      100 |   139.45 ns |   7.786 ns |  11.413 ns | 0.1044 |     - |     - |     328 B |
|  stringConcatFormat |      100 |    98.77 ns |   6.621 ns |   9.706 ns | 0.0867 |     - |     - |     272 B |
| stringBuilderFormat |      100 |   242.09 ns |  10.401 ns |  15.245 ns | 0.2933 |     - |     - |     920 B |
|       **cachedsprintf** |     **1000** |   **411.85 ns** |  **30.029 ns** |  **44.946 ns** | **0.7186** |     **-** |     **-** |    **2256 B** |
|       sprintfFormat |     1000 |   439.63 ns |  22.467 ns |  33.627 ns | 0.7520 |     - |     - |    2360 B |
|  stringFormatFormat |     1000 | 1,112.68 ns |  71.489 ns | 100.217 ns | 2.0142 |     - |     - |    6320 B |
|    stringJoinFormat |     1000 |   331.94 ns |  12.008 ns |  17.601 ns | 0.6781 |     - |     - |    2128 B |
|  stringConcatFormat |     1000 |   364.69 ns |  19.490 ns |  27.952 ns | 0.6604 |     - |     - |    2072 B |
| stringBuilderFormat |     1000 |   855.19 ns |  86.410 ns | 129.335 ns | 2.0161 |     - |     - |    6320 B |
|       **cachedsprintf** |     **4096** | **1,046.53 ns** |  **44.110 ns** |  **66.022 ns** | **2.6875** |     **-** |     **-** |    **8448 B** |
|       sprintfFormat |     4096 | 1,033.09 ns |  53.636 ns |  78.619 ns | 2.7237 |     - |     - |    8552 B |
|  stringFormatFormat |     4096 | 2,405.31 ns | 138.021 ns | 202.310 ns | 7.9346 |     - |     - |   24904 B |
|    stringJoinFormat |     4096 |   910.52 ns |  54.507 ns |  81.584 ns | 2.6522 |     - |     - |    8320 B |
|  stringConcatFormat |     4096 | 1,072.03 ns | 133.310 ns | 195.404 ns | 2.6302 |     - |     - |    8264 B |
| stringBuilderFormat |     4096 | 2,553.03 ns |  88.133 ns | 129.185 ns | 7.9346 |     - |     - |   24896 B |
