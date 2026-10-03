``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Job-MAGDGV : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Server=True  Toolchain=.NET Core 11.0  RunStrategy=Throughput  

```
|                 Method |   Categories |     Mean |     Error |    StdDev |   Median |      Op/s | Ratio | RatioSD |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|----------------------- |------------- |---------:|----------:|----------:|---------:|----------:|------:|--------:|-------:|------:|------:|----------:|
|  MvcRequestPassThrough |  MVC request | 3.927 μs | 0.1512 μs | 0.4265 μs | 3.808 μs | 254,657.7 |  1.00 |    0.00 | 0.0610 |     - |     - |      3 KB |
|    MvcRequestValidated |  MVC request | 3.797 μs | 0.1021 μs | 0.2978 μs | 3.745 μs | 263,384.7 |  0.98 |    0.13 | 0.0610 |     - |     - |      3 KB |
|                        |              |          |           |           |          |           |       |         |        |       |       |           |
| MvcResponsePassThrough | MVC response | 1.384 μs | 0.0477 μs | 0.1383 μs | 1.358 μs | 722,317.4 |  1.00 |    0.00 | 0.0324 |     - |     - |      2 KB |
|   MvcResponseValidated | MVC response | 1.715 μs | 0.0475 μs | 0.1354 μs | 1.704 μs | 583,221.7 |  1.25 |    0.15 | 0.0324 |     - |     - |      2 KB |
