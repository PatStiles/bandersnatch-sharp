``` ini

BenchmarkDotNet=v0.13.2, OS=ubuntu 22.04
AMD EPYC 7713, 1 CPU, 8 logical and 8 physical cores
.NET SDK=6.0.110
  [Host]   : .NET 6.0.10 (6.0.1022.47701), X64 RyuJIT AVX2
  .NET 6.0 : .NET 6.0.10 (6.0.1022.47701), X64 RyuJIT AVX2

Job=.NET 6.0  Runtime=.NET 6.0  

```
|               Method |                  _a |         _d |               Mean |              Error |             StdDev |             Median | Ratio |      Gen0 |      Gen1 |      Gen2 |   Allocated | Alloc Ratio |
|--------------------- |-------------------- |----------- |-------------------:|-------------------:|-------------------:|-------------------:|------:|----------:|----------:|----------:|------------:|------------:|
| **LeftShift_BigInteger** | **(115(...)FpE) [204]** | **1559595546** | **619,279,403.268 ns** | **14,836,791.0958 ns** | **43,044,221.2388 ns** | **603,581,289.000 ns** | **1.000** | **1000.0000** | **1000.0000** | **1000.0000** | **389900496 B** |        **1.00** |
|    LeftShift_UInt256 | (115(...)FpE) [204] | 1559595546 |           4.175 ns |          0.1010 ns |          0.1122 ns |           4.167 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|    LeftShift_Element | (115(...)FpE) [204] | 1559595546 |           2.514 ns |          0.1159 ns |          0.1507 ns |           2.504 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|                      |                     |            |                    |                    |                    |                    |       |           |           |           |             |             |
| **LeftShift_BigInteger** | **(115(...)FpE) [204]** | **1649316166** | **707,463,889.824 ns** | **13,744,071.9052 ns** | **14,114,149.9443 ns** | **707,105,214.000 ns** | **1.000** | **1000.0000** | **1000.0000** | **1000.0000** | **412331296 B** |        **1.00** |
|    LeftShift_UInt256 | (115(...)FpE) [204] | 1649316166 |           4.238 ns |          0.1175 ns |          0.1042 ns |           4.182 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|    LeftShift_Element | (115(...)FpE) [204] | 1649316166 |           2.354 ns |          0.0980 ns |          0.0869 ns |           2.336 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|                      |                     |            |                    |                    |                    |                    |       |           |           |           |             |             |
| **LeftShift_BigInteger** | **(115(...)FpE) [204]** | **1755192844** | **763,921,076.333 ns** | **14,324,272.1051 ns** | **13,398,933.0145 ns** | **766,682,448.000 ns** | **1.000** | **1000.0000** | **1000.0000** | **1000.0000** | **438799968 B** |        **1.00** |
|    LeftShift_UInt256 | (115(...)FpE) [204] | 1755192844 |           4.184 ns |          0.1407 ns |          0.1564 ns |           4.189 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|    LeftShift_Element | (115(...)FpE) [204] | 1755192844 |           2.505 ns |          0.1133 ns |          0.1924 ns |           2.447 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|                      |                     |            |                    |                    |                    |                    |       |           |           |           |             |             |
| **LeftShift_BigInteger** | **(619(...)FpE) [200]** | **1559595546** | **612,873,590.722 ns** | **13,590,944.6390 ns** | **39,429,794.7653 ns** | **595,120,013.000 ns** | **1.000** | **1000.0000** | **1000.0000** | **1000.0000** | **389901168 B** |        **1.00** |
|    LeftShift_UInt256 | (619(...)FpE) [200] | 1559595546 |           4.321 ns |          0.1273 ns |          0.1191 ns |           4.336 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|    LeftShift_Element | (619(...)FpE) [200] | 1559595546 |           2.584 ns |          0.1177 ns |          0.1156 ns |           2.574 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|                      |                     |            |                    |                    |                    |                    |       |           |           |           |             |             |
| **LeftShift_BigInteger** | **(619(...)FpE) [200]** | **1649316166** | **713,837,539.450 ns** | **13,588,869.2626 ns** | **15,648,962.0505 ns** | **712,005,500.500 ns** | **1.000** | **1000.0000** | **1000.0000** | **1000.0000** | **412330656 B** |        **1.00** |
|    LeftShift_UInt256 | (619(...)FpE) [200] | 1649316166 |           4.096 ns |          0.0899 ns |          0.0751 ns |           4.083 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|    LeftShift_Element | (619(...)FpE) [200] | 1649316166 |           2.593 ns |          0.1094 ns |          0.2185 ns |           2.547 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|                      |                     |            |                    |                    |                    |                    |       |           |           |           |             |             |
| **LeftShift_BigInteger** | **(619(...)FpE) [200]** | **1755192844** | **745,084,497.467 ns** | **13,403,757.7491 ns** | **12,537,883.3148 ns** | **740,590,129.000 ns** | **1.000** | **1000.0000** | **1000.0000** | **1000.0000** | **438800464 B** |        **1.00** |
|    LeftShift_UInt256 | (619(...)FpE) [200] | 1755192844 |           4.506 ns |          0.1547 ns |          0.2542 ns |           4.395 ns | 0.000 |         - |         - |         - |           - |        0.00 |
|    LeftShift_Element | (619(...)FpE) [200] | 1755192844 |           2.577 ns |          0.0823 ns |          0.0687 ns |           2.546 ns | 0.000 |         - |         - |         - |           - |        0.00 |
