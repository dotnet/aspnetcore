``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]   : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  ShortRun : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
|               Method |           Mean |           Error |        StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------- |---------------:|----------------:|--------------:|------:|--------:|-------:|-------:|------:|----------:|
| NativeExporterBundle | 36,096.1856 ns |  12,197.0531 ns |   668.5616 ns | 1.150 |    0.26 | 0.9766 |      - |     - |  50,360 B |
| SamePassBundleAccess |      0.3142 ns |       0.2467 ns |     0.0135 ns | 0.000 |    0.00 |      - |      - |     - |         - |
|        CompileCorvus | 32,402.5728 ns | 123,124.8841 ns | 6,748.8904 ns | 1.000 |    0.00 | 0.9766 |      - |     - |  52,600 B |
|      LoadCorvusImage | 12,550.2337 ns |   6,811.1712 ns |   373.3433 ns | 0.398 |    0.08 | 0.8240 | 0.0610 |     - |  39,190 B |
|   CorvusBindingValid |    326.1590 ns |      72.9484 ns |     3.9985 ns | 0.010 |    0.00 | 0.0033 |      - |     - |     168 B |
