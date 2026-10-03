``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Job-JTFPWI : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Server=True  Toolchain=.NET Core 11.0  RunStrategy=Throughput

```
|                Method |        Mean |     Error |     StdDev |         Op/s | Ratio |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|---------------------- |------------:|----------:|-----------:|-------------:|------:|-------:|------:|------:|----------:|
|   RuntimeRegistration | 3,010.09 ns | 59.733 ns | 146.526 ns |    332,216.5 | 1.000 | 0.0763 |     - |     - |   4,088 B |
| GeneratedRegistration |    23.04 ns |  0.484 ns |   1.169 ns | 43,411,055.7 | 0.008 | 0.0017 |     - |     - |      80 B |
