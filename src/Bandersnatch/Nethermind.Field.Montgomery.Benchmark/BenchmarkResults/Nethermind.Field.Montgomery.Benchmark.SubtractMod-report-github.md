``` ini

BenchmarkDotNet=v0.13.2, OS=ubuntu 22.04
AMD EPYC 7713, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.110
  [Host]   : .NET 6.0.10 (6.0.1022.47701), X64 RyuJIT AVX2
  .NET 6.0 : .NET 6.0.10 (6.0.1022.47701), X64 RyuJIT AVX2

Job=.NET 6.0  Runtime=.NET 6.0  

```
|                 Method |                  _a |                  _b |       Mean |     Error |    StdDev | Ratio | RatioSD |   Gen0 | Allocated | Alloc Ratio |
|----------------------- |-------------------- |-------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **SubtractMod_BigInteger** | **(115(...)FpE) [204]** | **(115(...)FpE) [204]** |  **63.251 ns** | **1.2663 ns** | **2.0449 ns** |  **1.00** |    **0.00** | **0.0006** |      **56 B** |        **1.00** |
|    SubtractMod_UInt256 | (115(...)FpE) [204] | (115(...)FpE) [204] |  10.724 ns | 0.2684 ns | 0.3296 ns |  0.17 |    0.01 |      - |         - |        0.00 |
|    SubtractMod_Element | (115(...)FpE) [204] | (115(...)FpE) [204] |   9.354 ns | 0.1110 ns | 0.0984 ns |  0.15 |    0.01 |      - |         - |        0.00 |
|                        |                     |                     |            |           |           |       |         |        |           |             |
| **SubtractMod_BigInteger** | **(115(...)FpE) [204]** | **(619(...)FpE) [200]** | **101.775 ns** | **1.9000 ns** | **1.7772 ns** |  **1.00** |    **0.00** | **0.0013** |     **112 B** |        **1.00** |
|    SubtractMod_UInt256 | (115(...)FpE) [204] | (619(...)FpE) [200] |  89.925 ns | 1.8597 ns | 2.4826 ns |  0.88 |    0.02 |      - |         - |        0.00 |
|    SubtractMod_Element | (115(...)FpE) [204] | (619(...)FpE) [200] |  13.412 ns | 0.2527 ns | 0.2240 ns |  0.13 |    0.00 |      - |         - |        0.00 |
|                        |                     |                     |            |           |           |       |         |        |           |             |
| **SubtractMod_BigInteger** | **(619(...)FpE) [200]** | **(115(...)FpE) [204]** | **101.471 ns** | **1.2030 ns** | **1.0664 ns** |  **1.00** |    **0.00** | **0.0013** |     **112 B** |        **1.00** |
|    SubtractMod_UInt256 | (619(...)FpE) [200] | (115(...)FpE) [204] |  94.184 ns | 1.8786 ns | 2.1634 ns |  0.93 |    0.02 |      - |         - |        0.00 |
|    SubtractMod_Element | (619(...)FpE) [200] | (115(...)FpE) [204] |   9.361 ns | 0.1070 ns | 0.1001 ns |  0.09 |    0.00 |      - |         - |        0.00 |
|                        |                     |                     |            |           |           |       |         |        |           |             |
| **SubtractMod_BigInteger** | **(619(...)FpE) [200]** | **(619(...)FpE) [200]** |  **63.099 ns** | **1.2539 ns** | **1.9522 ns** |  **1.00** |    **0.00** | **0.0006** |      **56 B** |        **1.00** |
|    SubtractMod_UInt256 | (619(...)FpE) [200] | (619(...)FpE) [200] |  10.502 ns | 0.1751 ns | 0.1367 ns |  0.17 |    0.01 |      - |         - |        0.00 |
|    SubtractMod_Element | (619(...)FpE) [200] | (619(...)FpE) [200] |   9.319 ns | 0.0775 ns | 0.0647 ns |  0.15 |    0.01 |      - |         - |        0.00 |
