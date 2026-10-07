``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]   : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  ShortRun : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
|                          Method |       Mean |      Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |-----------:|-----------:|----------:|-------:|------:|------:|----------:|
| AspNetJsonSchemaNetBindingValid | 9,283.3 ns | 3,116.2 ns | 170.81 ns | 0.2441 |     - |     - |  12,816 B |
|        AspNetCorvusBindingValid |   359.1 ns |   180.6 ns |   9.90 ns | 0.0033 |     - |     - |     168 B |
