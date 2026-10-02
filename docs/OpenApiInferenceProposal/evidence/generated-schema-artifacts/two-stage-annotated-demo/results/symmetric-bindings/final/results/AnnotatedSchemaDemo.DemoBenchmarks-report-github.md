``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  DefaultJob : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT


```
|                          Method |       Mean |     Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------------- |-----------:|----------:|----------:|-------:|------:|------:|----------:|
| AspNetJsonSchemaNetBindingValid | 8,433.5 ns | 194.65 ns | 573.92 ns | 0.2441 |     - |     - |  12,816 B |
|        AspNetCorvusBindingValid |   384.8 ns |   7.54 ns |  13.59 ns | 0.0033 |     - |     - |     168 B |
