``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Job-JTFPWI : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Server=True  Toolchain=.NET Core 11.0  RunStrategy=Throughput

```
|                                  Method |      Mean |    Error |    StdDev |    Median |         Op/s | Ratio | RatioSD | Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------------------------- |----------:|---------:|----------:|----------:|-------------:|------:|--------:|------:|------:|------:|----------:|
|                             PassThrough |  13.18 ns | 0.320 ns |  0.902 ns |  12.95 ns | 75,855,231.2 |  1.00 |    0.00 |     - |     - |     - |         - |
|                  ValidatedFrameworkOnly | 131.17 ns | 4.030 ns | 11.233 ns | 130.97 ns |  7,623,841.0 |  9.97 |    0.99 |     - |     - |     - |         - |
|          ValidatedFrameworkOnlyResponse | 201.43 ns | 8.239 ns | 23.640 ns | 194.60 ns |  4,964,529.5 | 15.31 |    2.17 |     - |     - |     - |         - |
|         GeneratedValidatedFrameworkOnly | 117.10 ns | 2.325 ns |  4.905 ns | 116.30 ns |  8,539,917.4 |  8.95 |    0.76 |     - |     - |     - |         - |
| GeneratedValidatedFrameworkOnlyResponse | 186.25 ns | 3.720 ns |  5.791 ns | 185.49 ns |  5,369,257.4 | 14.46 |    0.87 |     - |     - |     - |         - |
