``` ini

BenchmarkDotNet=v0.13.0, OS=ubuntu 22.04
13th Gen Intel Core i7-13800H, 1 CPU, 20 logical and 10 physical cores
.NET SDK=11.0.100-rc.1.26420.103
  [Host]     : .NET 11.0.0 (11.0.26.45408), X64 RyuJIT
  DefaultJob : .NET 11.0.0 (11.0.26.42103), X64 RyuJIT


```
|               Method |           Mean |       Error |        StdDev | Ratio | RatioSD |  Gen 0 |  Gen 1 | Gen 2 | Allocated |
|--------------------- |---------------:|------------:|--------------:|------:|--------:|-------:|-------:|------:|----------:|
| NativeExporterBundle | 29,879.6181 ns | 586.2279 ns | 1,011.2143 ns | 1.342 |    0.05 | 0.9766 |      - |     - |  50,360 B |
| SamePassBundleAccess |      0.2938 ns |   0.0099 ns |     0.0106 ns | 0.000 |    0.00 |      - |      - |     - |         - |
|        CompileCorvus | 22,192.0935 ns | 428.2992 ns |   586.2607 ns | 1.000 |    0.00 | 1.0986 |      - |     - |  52,600 B |
|      LoadCorvusImage | 12,132.1235 ns | 208.5482 ns |   174.1472 ns | 0.548 |    0.02 | 0.8240 | 0.0763 |     - |  39,160 B |
|   CorvusBindingValid |    321.1052 ns |   3.4680 ns |     3.2440 ns | 0.014 |    0.00 | 0.0033 |      - |     - |     168 B |
