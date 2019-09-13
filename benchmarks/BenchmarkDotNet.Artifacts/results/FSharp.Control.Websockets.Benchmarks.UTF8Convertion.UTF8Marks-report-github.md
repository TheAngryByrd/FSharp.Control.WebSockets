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
|            Method | dataSize |           Mean |         Error |        StdDev |         Median |     Gen 0 |     Gen 1 | Gen 2 |   Allocated |
|------------------ |--------- |---------------:|--------------:|--------------:|---------------:|----------:|----------:|------:|------------:|
|        **UTFToBytes** |     **1000** |       **3.097 us** |     **0.1803 us** |     **0.2642 us** |       **3.032 us** |         **-** |         **-** |     **-** |     **3.02 KB** |
| UTFToStreamReader |     1000 |       5.304 us |     0.3225 us |     0.4727 us |       5.256 us |         - |         - |     - |    16.28 KB |
| UTFToCustomReader |     1000 |      27.723 us |     0.7381 us |     1.0103 us |      27.343 us |         - |         - |     - |     4.02 KB |
|        **UTFToBytes** |    **20000** |      **15.363 us** |     **1.0974 us** |     **1.5384 us** |      **15.760 us** |         **-** |         **-** |     **-** |    **58.69 KB** |
| UTFToStreamReader |    20000 |      19.515 us |     2.1434 us |     3.0739 us |      18.286 us |         - |         - |     - |    98.89 KB |
| UTFToCustomReader |    20000 |     517.668 us |    11.5572 us |    15.8197 us |     516.256 us |         - |         - |     - |    78.23 KB |
|        **UTFToBytes** |   **100000** |      **69.696 us** |     **9.2229 us** |    **13.5188 us** |      **65.309 us** |         **-** |         **-** |     **-** |   **293.06 KB** |
| UTFToStreamReader |   100000 |      82.205 us |     5.9312 us |     8.3147 us |      81.058 us |         - |         - |     - |   412.09 KB |
| UTFToCustomReader |   100000 |   2,629.057 us |    61.2416 us |    85.8522 us |   2,601.840 us |         - |         - |     - |   390.73 KB |
|        **UTFToBytes** |  **1000000** |     **864.580 us** |   **189.1921 us** |   **283.1738 us** |     **660.984 us** |         **-** |         **-** |     **-** |  **2929.78 KB** |
| UTFToStreamReader |  1000000 |   1,094.758 us |   177.2496 us |   265.2989 us |     963.478 us |         - |         - |     - |  3927.78 KB |
| UTFToCustomReader |  1000000 |  27,750.114 us |   296.4201 us |   443.6676 us |  27,810.071 us |         - |         - |     - |  3906.36 KB |
|        **UTFToBytes** | **10000000** |  **11,142.042 us** | **2,476.0776 us** | **3,706.0764 us** |   **8,268.378 us** |         **-** |         **-** |     **-** | **29296.97 KB** |
| UTFToStreamReader | 10000000 |  20,303.501 us | 2,155.7926 us | 3,226.6889 us |  18,998.266 us | 3000.0000 | 1000.0000 |     - | 39163.13 KB |
| UTFToCustomReader | 10000000 | 265,586.983 us | 3,464.7689 us | 5,078.6134 us | 265,654.986 us |         - |         - |     - | 39062.61 KB |
