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
|            Method | dataSize |          Mean |         Error |        StdDev |        Median |     Gen 0 |     Gen 1 | Gen 2 |   Allocated |
|------------------ |--------- |--------------:|--------------:|--------------:|--------------:|----------:|----------:|------:|------------:|
|        **UTFToBytes** |     **1000** |      **3.623 us** |     **0.5697 us** |     **0.8350 us** |      **3.317 us** |         **-** |         **-** |     **-** |     **3.02 KB** |
| UTFToStreamReader |     1000 |      4.993 us |     0.3336 us |     0.4994 us |      4.935 us |         - |         - |     - |    16.28 KB |
|        **UTFToBytes** |    **20000** |     **15.700 us** |     **1.1792 us** |     **1.7650 us** |     **15.812 us** |         **-** |         **-** |     **-** |    **58.69 KB** |
| UTFToStreamReader |    20000 |     19.367 us |     0.7748 us |     1.1112 us |     19.709 us |         - |         - |     - |    98.89 KB |
|        **UTFToBytes** |   **100000** |     **61.277 us** |     **2.5709 us** |     **3.6872 us** |     **60.227 us** |         **-** |         **-** |     **-** |   **293.06 KB** |
| UTFToStreamReader |   100000 |     78.203 us |     3.4074 us |     4.8868 us |     76.045 us |         - |         - |     - |   412.09 KB |
|        **UTFToBytes** |  **1000000** |    **897.462 us** |   **215.0929 us** |   **321.9409 us** |    **657.515 us** |         **-** |         **-** |     **-** |  **2929.78 KB** |
| UTFToStreamReader |  1000000 |  1,162.589 us |   212.5062 us |   318.0692 us |  1,208.514 us |         - |         - |     - |  3927.78 KB |
|        **UTFToBytes** | **10000000** | **10,598.489 us** | **2,421.6387 us** | **3,624.5948 us** |  **8,240.141 us** |         **-** |         **-** |     **-** | **29296.97 KB** |
| UTFToStreamReader | 10000000 | 19,871.572 us | 2,242.5016 us | 3,356.4709 us | 18,123.978 us | 3000.0000 | 1000.0000 |     - | 39163.13 KB |
