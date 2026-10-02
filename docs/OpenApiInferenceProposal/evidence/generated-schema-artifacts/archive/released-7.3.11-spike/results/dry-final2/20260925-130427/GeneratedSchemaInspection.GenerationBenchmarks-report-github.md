``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host] : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Dry    : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Job=Dry  IterationCount=1  LaunchCount=1
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1

```
|                     Method |       Mean | Error | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------------- |-----------:|------:|------:|------:|------:|------:|----------:|
|      WarmStaticFieldAccess |   260.1 μs |    NA |  1.00 |     - |     - |     - |      2 KB |
| RebuildNativeResourceGraph | 1,481.7 μs |    NA |  5.70 |     - |     - |     - |     60 KB |
|          SerializeRootOnly |   352.7 μs |    NA |  1.36 |     - |     - |     - |      2 KB |
|  CopyManualCanonicalBundle |   464.8 μs |    NA |  1.79 |     - |     - |     - |      2 KB |
|      CreateSupportedBundle |   855.4 μs |    NA |  3.29 |     - |     - |     - |     64 KB |
|       ParseSupportedBundle |   768.7 μs |    NA |  2.96 |     - |     - |     - |     59 KB |
|    CompileCorvusFromBundle |   311.1 μs |    NA |  1.20 |     - |     - |     - |      5 KB |
|                NativeValid |   786.7 μs |    NA |  3.02 |     - |     - |     - |     22 KB |
|              NativeInvalid |   790.1 μs |    NA |  3.04 |     - |     - |     - |     28 KB |
|          ParsedBundleValid |   790.7 μs |    NA |  3.04 |     - |     - |     - |     27 KB |
|        ParsedBundleInvalid | 1,596.5 μs |    NA |  6.14 |     - |     - |     - |     35 KB |
|          CorvusBundleValid |   727.8 μs |    NA |  2.80 |     - |     - |     - |      2 KB |
|        CorvusBundleInvalid |   342.3 μs |    NA |  1.32 |     - |     - |     - |      2 KB |
