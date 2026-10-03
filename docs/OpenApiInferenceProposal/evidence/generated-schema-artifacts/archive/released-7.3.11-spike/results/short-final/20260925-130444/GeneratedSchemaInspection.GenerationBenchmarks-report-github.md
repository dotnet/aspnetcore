``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]   : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  ShortRun : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
|                     Method |           Mean |           Error |         StdDev |     Ratio |   RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------------- |---------------:|----------------:|---------------:|----------:|----------:|-------:|-------:|------:|----------:|
|      WarmStaticFieldAccess |      0.6633 ns |       1.0906 ns |      0.0598 ns |      1.00 |      0.00 |      - |      - |     - |         - |
| RebuildNativeResourceGraph | 40,050.7877 ns | 207,855.8784 ns | 11,393.2821 ns | 60,677.52 | 17,001.79 | 4.6387 | 1.4648 |     - |  58,720 B |
|          SerializeRootOnly |    626.9299 ns |   1,470.8585 ns |     80.6227 ns |    952.07 |    160.15 | 0.0496 |      - |     - |     624 B |
|  CopyManualCanonicalBundle |     47.9433 ns |     118.1180 ns |      6.4744 ns |     73.27 |     16.87 | 0.0746 |      - |     - |     936 B |
|      CreateSupportedBundle | 41,528.1809 ns | 193,451.7203 ns | 10,603.7416 ns | 63,000.44 | 16,462.05 | 4.8828 | 2.4414 |     - |  63,216 B |
|       ParseSupportedBundle | 30,362.1695 ns |  71,431.1932 ns |  3,915.3847 ns | 46,032.37 |  7,014.82 | 4.6387 | 2.1973 |     - |  58,496 B |
|    CompileCorvusFromBundle |    306.4259 ns |     594.5196 ns |     32.5876 ns |    467.74 |     95.98 | 0.2766 | 0.0019 |     - |   3,472 B |
|                NativeValid |  9,693.6298 ns |   5,762.0119 ns |    315.8353 ns | 14,686.35 |  1,263.12 | 1.5259 |      - |     - |  19,641 B |
|              NativeInvalid | 12,694.5641 ns |   6,587.8719 ns |    361.1035 ns | 19,231.31 |  1,590.71 | 2.0142 | 0.0610 |     - |  25,458 B |
|          ParsedBundleValid | 14,317.1873 ns |  34,507.2977 ns |  1,891.4614 ns | 21,658.37 |  2,866.46 | 1.9531 | 0.0610 |     - |  24,818 B |
|        ParsedBundleInvalid | 23,792.2989 ns | 132,215.0892 ns |  7,247.1552 ns | 36,023.91 | 10,656.30 | 2.5635 | 0.1221 |     - |  32,578 B |
|          CorvusBundleValid |    253.8483 ns |     460.1708 ns |     25.2235 ns |    387.25 |     76.19 | 0.0134 |      - |     - |     168 B |
|        CorvusBundleInvalid |    194.2290 ns |      43.2924 ns |      2.3730 ns |    294.45 |     27.03 | 0.0134 |      - |     - |     168 B |
