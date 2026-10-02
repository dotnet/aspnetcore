``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host] : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  Dry    : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Job=Dry  IterationCount=1  LaunchCount=1
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1

```
|               Method |      Mean | Error | Ratio | Gen 0 | Gen 1 | Gen 2 | Allocated |
|--------------------- |----------:|------:|------:|------:|------:|------:|----------:|
| NativeExporterBundle | 23.235 ms |    NA | 13.88 |     - |     - |     - |     51 KB |
| SamePassBundleAccess | 15.944 ms |    NA |  9.52 |     - |     - |     - |      1 KB |
|        CompileCorvus |  1.674 ms |    NA |  1.00 |     - |     - |     - |     52 KB |
|      LoadCorvusImage |  1.610 ms |    NA |  0.96 |     - |     - |     - |     40 KB |
|   CorvusBindingValid |  8.458 ms |    NA |  5.05 |     - |     - |     - |      1 KB |
